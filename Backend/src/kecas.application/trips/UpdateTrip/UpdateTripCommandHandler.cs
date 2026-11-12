namespace kecas.application.trips.UpdateTrip;

using System.Text.Json;
using kecas.application.abstractions.data;
using kecas.domain.fleet;
using kecas.domain.trips;

internal sealed class UpdateTripCommandHandler(
    ITripRepository tripRepository,
    IRouteRepository routeRepository,
    IVehicleRepository vehicleRepository,
    IDriverRepository driverRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateTripCommand>
{
    public async Task<Result> Handle(UpdateTripCommand command, CancellationToken cancellationToken)
    {
        var trip = await tripRepository.GetByIdAsync(command.TripId, cancellationToken);
        if (trip is null)
        {
            return Result.Failure(TripErrors.NotFound);
        }

        // Stale row_version → 409 (§6.1). The trip was already loaded, so a
        // mismatch means another session saved in between.
        if (command.RowVersion is { } rowVersion && trip.RowVersion != rowVersion)
        {
            return Result.Failure(TripErrors.RowVersionConflict);
        }

        if (trip.Status != TripStatus.Scheduled)
        {
            return Result.Failure(TripErrors.NotEditable);
        }

        var before = JsonSerializer.Serialize(new
        {
            startsAt = trip.StartsAt,
            endsAt = trip.EndsAt,
            routeId = trip.RouteId,
            vehicleId = trip.VehicleId,
            driverId = trip.DriverId,
        });

        // §7.3 step 1: route ACTIVE when changed.
        var targetRouteId = command.RouteId is { } r && r != Guid.Empty ? r : trip.RouteId;
        var route = await routeRepository.GetByIdAsync(targetRouteId, cancellationToken);
        if (route is null)
        {
            return Result.Failure(NetworkErrors.RouteNotFound);
        }

        if (!route.IsActive)
        {
            return Result.Failure(TripErrors.RouteInactive);
        }

        // §7.3 step 2: vehicle ACTIVE + category matches the (possibly new) route.
        var targetVehicleId = command.VehicleId ?? trip.VehicleId;
        if (targetVehicleId is { } vehicleId && vehicleId != Guid.Empty)
        {
            var vehicle = await vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
            if (vehicle is null)
            {
                return Result.Failure(VehicleErrors.NotFound);
            }

            if (vehicle.Status != VehicleStatus.Active
                || vehicle.Category.ToStorageValue() != route.Category.ToString().ToUpperInvariant())
            {
                return Result.Failure(TripErrors.VehicleNotAssignable);
            }
        }

        // §7.3 step 3: driver ACTIVE with an unexpired licence.
        var targetDriverId = command.DriverId ?? trip.DriverId;
        if (targetDriverId is { } driverId && driverId != Guid.Empty)
        {
            var driver = await driverRepository.GetByIdAsync(driverId, cancellationToken);
            if (driver is null)
            {
                return Result.Failure(DriverErrors.NotFound);
            }

            if (!driver.CanCheckIn(DateOnly.FromDateTime(command.StartsAtUtc ?? trip.StartsAt)))
            {
                return Result.Failure(TripErrors.DriverNotAssignable);
            }
        }

        var rescheduleResult = trip.Reschedule(
            command.StartsAtUtc, command.EndsAtUtc, command.RouteId, command.VehicleId, command.DriverId);
        if (rescheduleResult.IsFailure)
        {
            return rescheduleResult;
        }

        // §7.3 step 4 (friendly check): overlap, excluding this trip itself.
        var overlap = await tripRepository.FindOverlappingAsync(
            trip.DriverId, trip.VehicleId, trip.StartsAt, trip.EndsAt,
            excludeTripId: trip.Id, cancellationToken: cancellationToken);
        if (overlap is not null)
        {
            return Result.Failure(TripErrors.TripOverlap);
        }

        auditWriter.Append(
            command.ActorId,
            "trip.updated",
            "trip",
            trip.Id.ToString(),
            before,
            JsonSerializer.Serialize(new
            {
                startsAt = trip.StartsAt,
                endsAt = trip.EndsAt,
                routeId = trip.RouteId,
                vehicleId = trip.VehicleId,
                driverId = trip.DriverId,
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
