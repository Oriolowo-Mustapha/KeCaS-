namespace kecas.application.trips.CreateTrip;

using System.Text.Json;
using kecas.application.abstractions.data;
using kecas.domain.fleet;
using kecas.domain.trips;

internal sealed class CreateTripCommandHandler(
    ITripRepository tripRepository,
    IRouteRepository routeRepository,
    IVehicleRepository vehicleRepository,
    IDriverRepository driverRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateTripCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTripCommand command, CancellationToken cancellationToken)
    {
        // §7.3 step 1: route exists and is ACTIVE.
        var route = await routeRepository.GetByIdAsync(command.RouteId, cancellationToken);
        if (route is null)
        {
            return Result.Failure<Guid>(NetworkErrors.RouteNotFound);
        }

        if (!route.IsActive)
        {
            return Result.Failure<Guid>(TripErrors.RouteInactive);
        }

        // §7.3 step 2: vehicle ACTIVE and its category matches the route (BR-02).
        var vehicle = await vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return Result.Failure<Guid>(VehicleErrors.NotFound);
        }

        if (vehicle.Status != VehicleStatus.Active
            || vehicle.Category.ToStorageValue() != route.Category.ToString().ToUpperInvariant())
        {
            return Result.Failure<Guid>(TripErrors.VehicleNotAssignable);
        }

        // §7.3 step 3: driver ACTIVE with an unexpired licence.
        var driver = await driverRepository.GetByIdAsync(command.DriverId, cancellationToken);
        if (driver is null)
        {
            return Result.Failure<Guid>(DriverErrors.NotFound);
        }

        if (!driver.CanCheckIn(DateOnly.FromDateTime(command.StartsAtUtc)))
        {
            return Result.Failure<Guid>(TripErrors.DriverNotAssignable);
        }

        // §7.3 step 4 (friendly check): no overlapping live trip for the
        // driver or the vehicle — the exclusion constraints are the final guard.
        var overlap = await tripRepository.FindOverlappingAsync(
            command.DriverId, command.VehicleId, command.StartsAtUtc, command.EndsAtUtc, cancellationToken: cancellationToken);
        if (overlap is not null)
        {
            return Result.Failure<Guid>(TripErrors.TripOverlap);
        }

        var tripCode = await GenerateCodeAsync(tripRepository, cancellationToken);

        var tripResult = command.Source == TripSource.Logged
            ? Trip.LogCompleted(
                command.RouteId, command.VehicleId, command.DriverId,
                command.StartsAtUtc, command.EndsAtUtc,
                () => tripCode)
            : Trip.Schedule(
                command.RouteId, command.VehicleId, command.DriverId,
                command.StartsAtUtc, command.EndsAtUtc,
                () => tripCode);
        if (tripResult.IsFailure)
        {
            return Result.Failure<Guid>(tripResult.Error);
        }

        tripRepository.Add(tripResult.Value);

        auditWriter.Append(
            command.ActorId,
            "trip.created",
            "trip",
            tripResult.Value.Id.ToString(),
            after: JsonSerializer.Serialize(new
            {
                tripCode = tripResult.Value.TripCode,
                routeId = tripResult.Value.RouteId,
                vehicleId = tripResult.Value.VehicleId,
                driverId = tripResult.Value.DriverId,
                source = tripResult.Value.Source.ToStorageValue(),
                status = tripResult.Value.Status.ToStorageValue(),
                startsAt = tripResult.Value.StartsAt,
                endsAt = tripResult.Value.EndsAt,
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return tripResult.Value.Id;
    }

    /// <summary>Generates a unique TRP- code (BR-01).</summary>
    private static async Task<string> GenerateCodeAsync(
        ITripRepository tripRepository,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            var candidate = $"TRP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            if (await tripRepository.GetByCodeAsync(candidate, cancellationToken) is null)
            {
                return candidate;
            }
        }
    }
}
