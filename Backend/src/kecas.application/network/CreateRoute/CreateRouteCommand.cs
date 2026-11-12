namespace kecas.application.network.CreateRoute;

using kecas.domain.network;

/// <summary>POST /routes (FR-07): create a route with its ordered stop list.</summary>
public sealed record CreateRouteCommand(
    Guid ActorId,
    string Code,
    string Name,
    RouteCategory Category,
    string? Description,
    IReadOnlyList<RouteStopInput> Stops) : ICommand<Guid>;

/// <summary>One ordered stop on the route: 1-based sequence and minutes from the first stop.</summary>
public sealed record RouteStopInput(Guid StopId, int Seq, int MinsFromStart);
