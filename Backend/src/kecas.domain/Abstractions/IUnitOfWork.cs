namespace kecas.domain.abstractions;

/// <summary>
/// Implemented by the EF Core DbContext in Infrastructure.
/// Handlers commit through this so the Application layer never touches EF Core.
/// BeginTransactionAsync exists for multi-step saves that MUST land atomically
/// (§5.3: supersede the old published fare and publish the new one in one
/// transaction — a statement-level unique index needs the supersede UPDATE to
/// run before the publish UPDATE).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
