namespace kecas.domain.users;

/// <summary>
/// Repository for the User aggregate. Implementations must load roles and
/// their permissions together with the user (permission checks, FR-02).
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>One page of users ordered by full name (FR-03 list endpoint).</summary>
    Task<IReadOnlyList<User>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(User user);
}
