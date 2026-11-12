namespace kecas.application.trips.CompleteTrip;

/// <summary>
/// POST /trips/:id/complete (FR-15, §5.3): IN_PROGRESS → COMPLETED with the
/// actual end time. Managers (trips:manage) or the assigned driver (own trip).
/// </summary>
public sealed record CompleteTripCommand(
    Guid ActorId,
    IReadOnlyList<string> ActorPermissions,
    Guid TripId,
    DateTime? ActualEndUtc) : ICommand;
