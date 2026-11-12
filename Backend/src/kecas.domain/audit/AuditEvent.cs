namespace kecas.domain.audit;

using kecas.domain.abstractions;

/// <summary>
/// One append-only audit row (FR-20, NFR-14, DD-11). Audit rows are written in
/// the same database transaction as the change they describe, and the app can
/// never edit history — no update or delete paths exist by design.
/// </summary>
public sealed class AuditEvent : Entity
{
    private AuditEvent(
        Guid id,
        Guid? actorId,
        string action,
        string entityType,
        string? entityId,
        string? before,
        string? after,
        DateTime occurredAtUtc)
        : base(id)
    {
        ActorId = actorId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Before = before;
        After = after;
        OccurredAtUtc = occurredAtUtc;
    }

    private AuditEvent()
    {
        // EF Core
    }

    /// <summary>The signed-in user who performed the action; null for system actions.</summary>
    public Guid? ActorId { get; private set; }

    /// <summary>Stable verb, e.g. user.created, vehicle.status_changed.</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Aggregate name, e.g. user, vehicle, fare.</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>String form of the affected entity's id.</summary>
    public string? EntityId { get; private set; }

    /// <summary>JSON snapshot of the state before the change (null on create).</summary>
    public string? Before { get; private set; }

    /// <summary>JSON snapshot of the state after the change (null on delete).</summary>
    public string? After { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public static AuditEvent Create(
        Guid? actorId,
        string action,
        string entityType,
        string? entityId,
        string? before,
        string? after,
        DateTime occurredAtUtc)
    {
        return new AuditEvent(
            Guid.NewGuid(),
            actorId,
            action.Trim().ToLowerInvariant(),
            entityType.Trim().ToLowerInvariant(),
            entityId,
            before,
            after,
            occurredAtUtc);
    }
}
