namespace kecas.application.trips.CreateTrip;

using kecas.domain.trips;

/// <summary>
/// POST /trips (FR-10/14/15): schedule a trip with an assigned driver and
/// vehicle (§7.3 guards), or log a walk-up run after the fact (source=LOGGED,
/// created directly as COMPLETED, §5.2).
/// </summary>
public sealed record CreateTripCommand(
    Guid ActorId,
    Guid RouteId,
    Guid VehicleId,
    Guid DriverId,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    TripSource Source) : ICommand<Guid>;
