namespace kecas.domain.fares;

/// <summary>
/// Repository for fares and the approval workflow (FR-08/09, §5.2). The
/// published-lookup methods power the supersede-and-publish transaction (§5.3).
/// </summary>
public interface IFareRepository
{
    Task<Fare?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Fare>> ListAsync(
        int page,
        int pageSize,
        FareStatus? status = null,
        Guid? routeId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        FareStatus? status = null,
        Guid? routeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The currently PUBLISHED fare for the same scope (one route, or one
    /// from/to zone pair), if any — the fare a new approval supersedes (§5.3).
    /// </summary>
    Task<Fare?> FindPublishedForScopeAsync(
        Guid? routeId,
        Guid? fromZoneId,
        Guid? toZoneId,
        CancellationToken cancellationToken = default);

    /// <summary>All PUBLISHED fares, for the public endpoint (FR-09, DD-10).</summary>
    Task<IReadOnlyList<Fare>> ListPublishedAsync(CancellationToken cancellationToken = default);

    void Add(Fare fare);
}
