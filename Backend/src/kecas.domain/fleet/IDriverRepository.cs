namespace kecas.domain.fleet;

/// <summary>Repository for the Driver aggregate (FR-06).</summary>
public interface IDriverRepository
{
    Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>The driver record linked to a signed-in user account, if any (§8.1 "own" scope).</summary>
    Task<Driver?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Driver>> ListAsync(
        int page,
        int pageSize,
        Guid? operatorId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Guid? operatorId = null,
        CancellationToken cancellationToken = default);

    void Add(Driver driver);
}
