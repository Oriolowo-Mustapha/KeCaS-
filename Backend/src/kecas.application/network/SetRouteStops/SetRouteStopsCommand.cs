namespace kecas.application.network.SetRouteStops;

using kecas.application.network.CreateRoute;

/// <summary>
/// PUT /routes/:id/stops (FR-07): replace the route's ordered stop list.
/// Existing fares are unaffected — they reference the route, not its stops.
/// </summary>
public sealed record SetRouteStopsCommand(
    Guid ActorId,
    Guid RouteId,
    IReadOnlyList<RouteStopInput> Stops) : ICommand;
