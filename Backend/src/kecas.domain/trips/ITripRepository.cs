namespace kecas.domain.trips;

/// <summary>
/// Repository for the Trip aggregate (FR-10/14/15). Overlap lookups back the
/// friendly 409 TRIP_OVERLAP; the database exclusion constraints (BR-03, DD-02)
/// remain the final guard.
/// </summary>
public interface ITripRepository
{
    Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Trip?> GetByCodeAsync(string tripCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Trip>> ListAsync(
        int page,
        int pageSize,
        TripStatus? status = null,
        Guid? routeId = null,
        Guid? vehicleId = null,
        Guid? driverId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        TripStatus? status = null,
        Guid? routeId = null,
        Guid? vehicleId = null,
        Guid? driverId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Any LIVE (non-cancelled) trip for the same driver or vehicle whose time
    /// range overlaps [startsAt, endsAt), optionally excluding one trip (self
    /// on reschedule). Used for the 409 with the clashing trip code.
    /// </summary>
    Task<Trip?> FindOverlappingAsync(
        Guid? driverId,
        Guid? vehicleId,
        DateTime startsAt,
        DateTime endsAt,
        Guid? excludeTripId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Live or soon-starting trips, for the availability view (§5.1).</summary>
    Task<IReadOnlyList<Trip>> ListLiveForAvailabilityAsync(
        DateTime nowUtc,
        DateTime horizonUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// §7.4 step 4 / BR-09: SELECT ... FOR UPDATE on the trip row, inside the
    /// caller's ambient transaction, so concurrent confirms serialize their
    /// capacity sum. False when the trip does not exist.
    /// </summary>
    Task<bool> TryLockAsync(Guid tripId, CancellationToken cancellationToken = default);

    void Add(Trip trip);
}
