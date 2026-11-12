namespace kecas.domain.users;

/// <summary>
/// A database-backed signed-in session (design doc §8.2). Staff and passenger
/// sessions expire after 8 hours idle or 24 hours total. Sessions live in the
/// database and are deleted on deactivation; only the token hash is stored.
/// </summary>
public sealed class Session : Entity
{
    private Session(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        DateTime idleExpiresAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        IdleExpiresAtUtc = idleExpiresAtUtc;
    }

    private Session()
    {
        // EF Core
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Hard ceiling: 24 hours from creation.</summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>Sliding ceiling: 8 hours since the last authenticated request.</summary>
    public DateTime IdleExpiresAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public static readonly TimeSpan TotalLifetime = TimeSpan.FromHours(24);

    public static readonly TimeSpan IdleLifetime = TimeSpan.FromHours(8);

    public static Session Create(Guid userId, SessionToken token, DateTime nowUtc) => new(
        Guid.NewGuid(),
        userId,
        token.Hash,
        nowUtc,
        nowUtc.Add(TotalLifetime),
        nowUtc.Add(IdleLifetime));

    public bool IsAlive(DateTime nowUtc) =>
        RevokedAtUtc is null && nowUtc < ExpiresAtUtc && nowUtc < IdleExpiresAtUtc;

    /// <summary>Extends the idle window on each authenticated request.</summary>
    public Result Touch(DateTime nowUtc)
    {
        if (!IsAlive(nowUtc))
        {
            return Result.Failure(AuthErrors.SessionInvalid);
        }

        IdleExpiresAtUtc = nowUtc.Add(IdleLifetime);
        return Result.Success();
    }

    public void Revoke(DateTime nowUtc)
    {
        RevokedAtUtc ??= nowUtc;
    }
}
