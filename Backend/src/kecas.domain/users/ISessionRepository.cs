namespace kecas.domain.users;

/// <summary>
/// Repository for Session entities (FR-01, design doc §8.2).
/// </summary>
public interface ISessionRepository
{
    Task<Session?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    void Add(Session session);

    /// <summary>Deactivation deletes the user's sessions immediately.</summary>
    Task RevokeAllForUserAsync(Guid userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default);

    /// <summary>Password change revokes every other session of the user.</summary>
    Task RevokeAllForUserExceptAsync(Guid userId, string keepTokenHash, DateTime revokedAtUtc, CancellationToken cancellationToken = default);
}
