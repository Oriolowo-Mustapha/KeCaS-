namespace kecas.application.trips;

using kecas.domain.network;
using kecas.domain.trips;

/// <summary>Shared response shapes for the trips module (FR-10/14/15).</summary>
public sealed record TripListItem(
    Guid Id,
    string TripCode,
    Guid RouteId,
    string RouteCode,
    Guid? VehicleId,
    string? VehicleCode,
    Guid? DriverId,
    string Source,
    string Status,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string? CancelReason);

public sealed record ListTripsResponse(IReadOnlyList<TripListItem> Items, int Page, int PageSize, int Total);

/// <summary>Maps a Trip aggregate to its API shape with route/vehicle codes resolved.</summary>
public static class TripMapper
{
    public static TripListItem ToListItem(
        Trip trip,
        IReadOnlyDictionary<Guid, Route> routeById,
        IReadOnlyDictionary<Guid, Vehicle> vehicleById)
    {
        var route = routeById.GetValueOrDefault(trip.RouteId);
        var vehicle = trip.VehicleId is { } vehicleId ? vehicleById.GetValueOrDefault(vehicleId) : null;

        return new TripListItem(
            trip.Id,
            trip.TripCode,
            trip.RouteId,
            route?.Code ?? string.Empty,
            trip.VehicleId,
            vehicle?.Code.Value,
            trip.DriverId,
            trip.Source.ToStorageValue(),
            trip.Status.ToStorageValue(),
            trip.StartsAt,
            trip.EndsAt,
            trip.CancelReason);
    }
}
