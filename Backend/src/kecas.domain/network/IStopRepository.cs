namespace kecas.domain.network;

/// <summary>
/// Repository for stops (FR-07). Implemented by Infrastructure on EF Core.
/// </summary>
public interface IStopRepository
{
    Task<Stop?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Stop>> ListAsync(Guid? zoneId = null, CancellationToken cancellationToken = default);

    /// <summary>Bulk lookup used by public route/availability projections.</summary>
    Task<IReadOnlyList<Stop>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    void Add(Stop stop);
}
