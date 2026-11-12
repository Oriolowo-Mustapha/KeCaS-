namespace kecas.domain.trips;

using kecas.domain.abstractions;

/// <summary>
/// A trip record (design doc §4.2, FR-10/14/15): route required (BR-04),
/// vehicle/driver set by assignment. Live trips may not overlap for one
/// driver or one vehicle (BR-03) — guarded by database exclusion constraints
/// (DD-02) plus friendly handler checks. row_version protects concurrent edits.
/// </summary>
public sealed class Trip : Entity
{
    private Trip(
        Guid id,
        string tripCode,
        Guid routeId,
        Guid? vehicleId,
        Guid? driverId,
        TripSource source,
        TripStatus status,
        DateTime startsAt,
        DateTime endsAt)
        : base(id)
    {
        TripCode = tripCode;
        RouteId = routeId;
        VehicleId = vehicleId;
        DriverId = driverId;
        Source = source;
        Status = status;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    private Trip()
    {
        // EF Core
    }

    /// <summary>Unique human-facing code, e.g. TRP-3F91A2C7 (BR-01).</summary>
    public string TripCode { get; private set; } = string.Empty;

    public Guid RouteId { get; private set; }

    public Guid? VehicleId { get; private set; }

    public Guid? DriverId { get; private set; }

    public TripSource Source { get; private set; }

    public TripStatus Status { get; private set; }

    public DateTime StartsAt { get; private set; }

    /// <summary>Planned end; overwritten with the ACTUAL end on completion (§5.3).</summary>
    public DateTime EndsAt { get; private set; }

    public string? CancelReason { get; private set; }

    /// <summary>Concurrency token for two-session edits (§4.2, 409 on staleness).</summary>
    public long RowVersion { get; private set; }

    public bool IsLive => Status is TripStatus.Scheduled or TripStatus.InProgress;

    /// <summary>§5.2: walk-up keke/cab runs logged after the fact — directly COMPLETED.</summary>
    public static Result<Trip> LogCompleted(
        Guid routeId,
        Guid? vehicleId,
        Guid? driverId,
        DateTime startedAtUtc,
        DateTime endedAtUtc,
        Func<string> codeFactory)
    {
        if (endedAtUtc <= startedAtUtc)
        {
            return Result.Failure<Trip>(TripErrors.EndBeforeStart);
        }

        return new Trip(
            Guid.NewGuid(),
            codeFactory(),
            routeId,
            vehicleId,
            driverId,
            TripSource.Logged,
            TripStatus.Completed,
            startedAtUtc,
            endedAtUtc);
    }

    /// <summary>A scheduled trip with an assigned driver and vehicle (§7.3).</summary>
    public static Result<Trip> Schedule(
        Guid routeId,
        Guid vehicleId,
        Guid driverId,
        DateTime startsAtUtc,
        DateTime endsAtUtc,
        Func<string> codeFactory)
    {
        if (endsAtUtc <= startsAtUtc)
        {
            return Result.Failure<Trip>(TripErrors.EndBeforeStart);
        }

        return new Trip(
            Guid.NewGuid(),
            codeFactory(),
            routeId,
            vehicleId,
            driverId,
            TripSource.Scheduled,
            TripStatus.Scheduled,
            startsAtUtc,
            endsAtUtc);
    }

    /// <summary>
    /// SCHEDULED → IN_PROGRESS (§5.3). The caller (application layer) enforces
    /// the guard: vehicle ACTIVE and on an open shift with the same driver.
    /// </summary>
    public Result Start()
    {
        if (Status != TripStatus.Scheduled)
        {
            return Result.Failure(TripErrors.NotStartable);
        }

        Status = TripStatus.InProgress;
        return Result.Success();
    }

    /// <summary>IN_PROGRESS → COMPLETED; sets the actual end time (§5.3).</summary>
    public Result Complete(DateTime actualEndUtc)
    {
        if (Status != TripStatus.InProgress)
        {
            return Result.Failure(TripErrors.NotCompletable);
        }

        if (actualEndUtc <= StartsAt)
        {
            return Result.Failure(TripErrors.EndBeforeStart);
        }

        EndsAt = actualEndUtc;
        Status = TripStatus.Completed;
        return Result.Success();
    }

    /// <summary>SCHEDULED or IN_PROGRESS → CANCELLED with a mandatory reason (§5.3).</summary>
    public Result Cancel(string reason)
    {
        if (!IsLive)
        {
            return Result.Failure(TripErrors.NotCancellable);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(TripErrors.ReasonRequired);
        }

        CancelReason = reason.Trim();
        Status = TripStatus.Cancelled;
        return Result.Success();
    }

    /// <summary>Reschedules or reassigns a SCHEDULED trip; overlap is re-checked by the caller.</summary>
    public Result Reschedule(
        DateTime? startsAtUtc,
        DateTime? endsAtUtc,
        Guid? routeId,
        Guid? vehicleId,
        Guid? driverId)
    {
        if (Status != TripStatus.Scheduled)
        {
            return Result.Failure(TripErrors.NotEditable);
        }

        var newStart = startsAtUtc ?? StartsAt;
        var newEnd = endsAtUtc ?? EndsAt;
        if (newEnd <= newStart)
        {
            return Result.Failure(TripErrors.EndBeforeStart);
        }

        if (routeId is { } r && r != Guid.Empty)
        {
            RouteId = r;
        }

        if (vehicleId is { } v)
        {
            VehicleId = v == Guid.Empty ? null : v;
        }

        if (driverId is { } d)
        {
            DriverId = d == Guid.Empty ? null : d;
        }

        StartsAt = newStart;
        EndsAt = newEnd;
        return Result.Success();
    }
}
