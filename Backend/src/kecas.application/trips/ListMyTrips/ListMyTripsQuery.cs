namespace kecas.application.trips.ListMyTrips;

using kecas.domain.trips;

/// <summary>
/// GET /trips/mine (§8.1 "own" scope): the signed-in driver's own trips, any
/// status. A user without a linked driver record is denied (403).
/// </summary>
public sealed record ListMyTripsQuery(
    Guid ActorId,
    int Page,
    int PageSize,
    TripStatus? Status) : IQuery<ListTripsResponse>;
