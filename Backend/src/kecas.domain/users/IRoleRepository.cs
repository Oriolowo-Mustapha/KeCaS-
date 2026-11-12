namespace kecas.domain.users;

/// <summary>Repository for roles (FR-03 role administration, seeding).</summary>
public interface IRoleRepository
{
    Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);
}
