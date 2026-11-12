namespace kecas.application.audit.ListAuditEvents;

/// <summary>
/// Admin reads the audit log (FR-20), filtered by actor, entity type and date
/// range (§6.2: GET /audit-events).
/// </summary>
public sealed record ListAuditEventsQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? ActorId = null,
    string? EntityType = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null) : IQuery<ListAuditEventsResponse>;
