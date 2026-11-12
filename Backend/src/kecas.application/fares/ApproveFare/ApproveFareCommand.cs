namespace kecas.application.fares.ApproveFare;

/// <summary>
/// POST /fares/:id/approve (FR-08/09): PENDING_APPROVAL → PUBLISHED. The
/// approver must differ from the proposer (§5.3); a currently published fare
/// for the same scope is superseded in the SAME transaction.
/// </summary>
public sealed record ApproveFareCommand(
    Guid ActorId,
    Guid FareId) : ICommand;
