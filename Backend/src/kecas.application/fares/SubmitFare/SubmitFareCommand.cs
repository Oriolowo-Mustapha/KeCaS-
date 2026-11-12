namespace kecas.application.fares.SubmitFare;

/// <summary>POST /fares/:id/submit (FR-08): DRAFT → PENDING_APPROVAL (§5.2).</summary>
public sealed record SubmitFareCommand(
    Guid ActorId,
    Guid FareId) : ICommand;
