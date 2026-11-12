namespace kecas.application.trips.StartTrip;

using System.Text.Json;
using kecas.application.abstractions.data;
using kecas.domain.fleet;
using kecas.domain.trips;

internal sealed class StartTripCommandHandler(
    ITripRepository tripRepository,
    IVehicleRepository vehicleRepository,
    IDriverRepository driverRepository,
    IShiftRepository shiftRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<StartTripCommand>
{
    private const string ManageTripsPermission = "trips:manage";

    public async Task<Result> Handle(StartTripCommand command, CancellationToken cancellationToken)
    {
        var trip = await tripRepository.GetByIdAsync(command.TripId, cancellationToken);
        if (trip is null)
        {
            return Result.Failure(TripErrors.NotFound);
        }

        // trips:manage, or the assigned driver acting on their own trip.
        if (!command.ActorPermissions.Contains(ManageTripsPermission))
        {
            var ownDriver = await driverRepository.GetByUserIdAsync(command.ActorId, cancellationToken);
            if (ownDriver is null || ownDriver.Id != trip.DriverId)
            {
                return Result.Failure(AuthErrors.PermissionDenied);
            }
        }

        // §5.3 start guard: vehicle still ACTIVE and on an open shift whose
        // driver is the trip's driver.
        if (trip.VehicleId is { } vehicleId)
        {
            var vehicle = await vehicleRepository.GetByIdAsync(vehicleId, cancellationToken);
            if (vehicle is null || !vehicle.CanBeAssigned)
            {
                return Result.Failure(TripErrors.ShiftRequired);
            }

            if (await shiftRepository.FindOpenByVehicleAsync(vehicleId, cancellationToken)
                is not { } openShift
                || openShift.DriverId != trip.DriverId)
            {
                return Result.Failure(TripErrors.ShiftRequired);
            }
        }
        else if (trip.DriverId is not null)
        {
            // Assigned driver but no vehicle — nothing to start from a rank.
            return Result.Failure(TripErrors.ShiftRequired);
        }

        var result = trip.Start();
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "trip.started",
            "trip",
            trip.Id.ToString(),
            before: JsonSerializer.Serialize(new { status = TripStatus.Scheduled.ToStorageValue() }),
            after: JsonSerializer.Serialize(new { status = trip.Status.ToStorageValue() }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
