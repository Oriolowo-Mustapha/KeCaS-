namespace kecas.domain.fleet;

/// <summary>
/// Repository for the Operator aggregate root (design doc §3.3 fleet module).
/// </summary>
public interface IOperatorRepository
{
    Task<Operator?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(Operator @operator);
}
