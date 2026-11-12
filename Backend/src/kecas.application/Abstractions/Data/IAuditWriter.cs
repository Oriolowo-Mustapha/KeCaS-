namespace kecas.application.abstractions.data;

/// <summary>
/// DD-11: audit rows are written in the SAME database transaction as the
/// change they describe. Append the row before SaveChangesAsync and it commits
/// atomically with the change — a change cannot succeed without its audit row.
/// </summary>
public interface IAuditWriter
{
    /// <param name="actorId">Signed-in user performing the action; null for system actions.</param>
    /// <param name="action">Stable verb, e.g. user.created, vehicle.status_changed.</param>
    /// <param name="entityType">Aggregate name, e.g. user, vehicle.</param>
    /// <param name="entityId">String form of the affected entity's id.</param>
    /// <param name="before">JSON state before the change (null on create).</param>
    /// <param name="after">JSON state after the change (null on delete).</param>
    void Append(
        Guid? actorId,
        string action,
        string entityType,
        string? entityId,
        string? before = null,
        string? after = null);
}
