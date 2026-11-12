namespace kecas.application.trips.CancelTrip;

/// <summary>
/// POST /trips/:id/cancel (FR-15, §5.3): a live (SCHEDULED or IN_PROGRESS)
/// trip → CANCELLED with a mandatory reason. Managers (trips:manage) or the
/// assigned driver aborting their own trip.
/// </summary>
public sealed record CancelTripCommand(
    Guid ActorId,
    IReadOnlyList<string> ActorPermissions,
    Guid TripId,
    string? Reason) : ICommand;
