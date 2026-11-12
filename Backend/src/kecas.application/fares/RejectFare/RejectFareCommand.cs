namespace kecas.application.fares.RejectFare;

/// <summary>POST /fares/:id/reject (FR-08): PENDING_APPROVAL → REJECTED (§5.2).</summary>
public sealed record RejectFareCommand(
    Guid ActorId,
    Guid FareId) : ICommand;
