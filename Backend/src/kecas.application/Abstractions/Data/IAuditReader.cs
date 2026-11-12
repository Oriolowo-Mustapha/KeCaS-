namespace kecas.application.abstractions.data;

/// <summary>One audit row as returned to the admin viewer (FR-20).</summary>
public sealed record AuditEventListItem(
    Guid Id,
    Guid? ActorId,
    string Action,
    string EntityType,
    string? EntityId,
    string? Before,
    string? After,
    DateTime OccurredAtUtc);

/// <summary>
/// Read access to the audit log for the admin endpoint (FR-20). Filtering by
/// actor, entity and date range (§6.2: GET /audit-events).
/// </summary>
public interface IAuditReader
{
    Task<(IReadOnlyList<AuditEventListItem> Items, int Total)> ListAsync(
        int page,
        int pageSize,
        Guid? actorId = null,
        string? entityType = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default);
}
