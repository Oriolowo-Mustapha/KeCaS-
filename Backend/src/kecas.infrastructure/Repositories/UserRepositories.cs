namespace kecas.infrastructure.Repositories;

using kecas.domain.users;
using Microsoft.EntityFrameworkCore;

internal sealed class UserRepository(ApplicationDbContext context) : IUserRepository
{
    private IQueryable<User> UsersWithRoles =>
        context.Users
            .Include(u => u.Roles)
            .ThenInclude(r => r.Permissions);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        UsersWithRoles.FirstOrDefaultAsync(u => u.Email == new Email(normalizedEmail), cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        UsersWithRoles.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<User>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        await UsersWithRoles
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        context.Users.CountAsync(cancellationToken);

    public void Add(User user) => context.Users.Add(user);
}

internal sealed class SessionRepository(ApplicationDbContext context) : ISessionRepository
{
    public Task<Session?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        context.Sessions.FirstOrDefaultAsync(s => s.TokenHash == tokenHash, cancellationToken);

    public void Add(Session session) => context.Sessions.Add(session);

    public Task RevokeAllForUserAsync(Guid userId, DateTime revokedAtUtc, CancellationToken cancellationToken = default) =>
        context.Sessions
            .Where(s => s.UserId == userId && s.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(s => s.RevokedAtUtc, revokedAtUtc),
                cancellationToken);

    public Task RevokeAllForUserExceptAsync(
        Guid userId,
        string keepTokenHash,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default) =>
        context.Sessions
            .Where(s => s.UserId == userId && s.RevokedAtUtc == null && s.TokenHash != keepTokenHash)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(s => s.RevokedAtUtc, revokedAtUtc),
                cancellationToken);
}
