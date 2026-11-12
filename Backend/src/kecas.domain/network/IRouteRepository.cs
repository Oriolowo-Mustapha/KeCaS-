namespace kecas.domain.network;

/// <summary>
/// Repository for routes and their ordered stop lists (FR-07). Reads that
/// feed public projections must load Stops together with the route.
/// </summary>
public interface IRouteRepository
{
    Task<Route?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Routes with their Stops collection populated.</summary>
    Task<IReadOnlyList<Route>> ListAsync(
        RouteCategory? category = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        RouteCategory? category = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    void Add(Route route);
}
