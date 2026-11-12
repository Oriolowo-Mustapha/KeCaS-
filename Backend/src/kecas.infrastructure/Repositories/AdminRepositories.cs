namespace kecas.infrastructure.Repositories;

using kecas.application.abstractions.data;
using kecas.domain.audit;
using kecas.domain.fleet;
using kecas.domain.users;
using Microsoft.EntityFrameworkCore;

internal sealed class RoleRepository(ApplicationDbContext context) : IRoleRepository
{
    public Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        context.Roles.FirstOrDefaultAsync(r => r.Code == code.ToUpperInvariant(), cancellationToken);

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Roles.OrderBy(r => r.Code).ToListAsync(cancellationToken);
}

internal sealed class DriverRepository(ApplicationDbContext context) : IDriverRepository
{
    public Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Drivers.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<Driver?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Driver>> ListAsync(
        int page,
        int pageSize,
        Guid? operatorId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Drivers.AsNoTracking().AsQueryable();

        if (operatorId.HasValue)
        {
            query = query.Where(d => d.OperatorId == operatorId.Value);
        }

        return await query
            .OrderBy(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(Guid? operatorId = null, CancellationToken cancellationToken = default)
    {
        var query = context.Drivers.AsQueryable();

        if (operatorId.HasValue)
        {
            query = query.Where(d => d.OperatorId == operatorId.Value);
        }

        return query.CountAsync(cancellationToken);
    }

    public void Add(Driver driver) => context.Drivers.Add(driver);
}

internal sealed class AuditWriter(ApplicationDbContext context) : IAuditWriter
{
    public void Append(
        Guid? actorId,
        string action,
        string entityType,
        string? entityId,
        string? before = null,
        string? after = null) =>
        context.AuditEvents.Add(AuditEvent.Create(
            actorId, action, entityType, entityId, before, after, DateTime.UtcNow));
}

internal sealed class AuditReader(ApplicationDbContext context) : IAuditReader
{
    public async Task<(IReadOnlyList<AuditEventListItem> Items, int Total)> ListAsync(
        int page,
        int pageSize,
        Guid? actorId = null,
        string? entityType = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.AuditEvents.AsNoTracking().AsQueryable();

        if (actorId.HasValue)
        {
            query = query.Where(a => a.ActorId == actorId.Value);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(a => a.OccurredAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(a => a.OccurredAtUtc <= toUtc.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditEventListItem(
                a.Id,
                a.ActorId,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.Before,
                a.After,
                a.OccurredAtUtc))
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
