namespace kecas.application.trips.StartTrip;

/// <summary>
/// POST /trips/:id/start (FR-15, §5.3): SCHEDULED → IN_PROGRESS. Managers
/// (trips:manage) or the assigned driver (own trip) may start. The guard:
/// the vehicle must be ACTIVE and on an open shift with the same driver.
/// </summary>
public sealed record StartTripCommand(
    Guid ActorId,
    IReadOnlyList<string> ActorPermissions,
    Guid TripId) : ICommand;
