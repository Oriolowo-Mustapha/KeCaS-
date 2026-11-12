namespace kecas.application.network.UpdateRoute;

/// <summary>
/// PATCH /routes/:id (FR-07): rename, re-describe or (de)activate a route.
/// Retired routes stay for history — they are deactivated, never deleted.
/// </summary>
public sealed record UpdateRouteCommand(
    Guid ActorId,
    Guid RouteId,
    string? Name,
    string? Description,
    bool? IsActive) : ICommand;
