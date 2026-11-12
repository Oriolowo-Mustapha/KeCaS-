namespace kecas.application.trips.ListTrips;

using kecas.domain.trips;

public sealed record ListTripsQuery(
    int Page,
    int PageSize,
    TripStatus? Status,
    Guid? RouteId,
    Guid? VehicleId,
    Guid? DriverId,
    DateTime? From,
    DateTime? To) : IQuery<ListTripsResponse>;
