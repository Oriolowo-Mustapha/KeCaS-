namespace kecas.domain.fleet;

using kecas.domain.abstractions;

/// <summary>
/// A driver's duty period with a vehicle. A shift is open while EndedAt is
/// null; one open shift per driver and per vehicle is enforced by partial
/// unique indexes in the database (design doc §4.2) — this entity guards the
/// state machine itself.
/// </summary>
public sealed class Shift : Entity
{
    private Shift(
        Guid id,
        Guid driverId,
        Guid vehicleId,
        Guid? rankStopId,
        DateTime startedAt)
        : base(id)
    {
        DriverId = driverId;
        VehicleId = vehicleId;
        RankStopId = rankStopId;
        StartedAt = startedAt;
        EndedAt = null;
        ClosedBy = null;
    }

    private Shift()
    {
        // EF Core
    }

    public Guid DriverId { get; private set; }

    public Guid VehicleId { get; private set; }

    /// <summary>Optional rank (a stop where vehicles wait) chosen at check-in.</summary>
    public Guid? RankStopId { get; private set; }

    public DateTime StartedAt { get; private set; }

    public DateTime? EndedAt { get; private set; }

    /// <summary>USER or AUTO — AUTO is set by the stale-shift job (DD-06 guard).</summary>
    public ShiftClosedBy? ClosedBy { get; private set; }

    public bool IsOpen => EndedAt is null;

    public static Result<Shift> CheckIn(
        Driver driver,
        Vehicle vehicle,
        DateTime startedAtUtc,
        Guid? rankStopId = null)
    {
        if (driver is null)
        {
            return Result.Failure<Shift>(DriverErrors.NotFound);
        }

        if (vehicle is null)
        {
            return Result.Failure<Shift>(VehicleErrors.NotFound);
        }

        if (!driver.CanCheckIn(DateOnly.FromDateTime(startedAtUtc)))
        {
            return Result.Failure<Shift>(DriverErrors.LicenceInvalid);
        }

        if (!vehicle.CanBeAssigned)
        {
            return Result.Failure<Shift>(VehicleErrors.NotAssignable);
        }

        var shift = new Shift(
            Guid.NewGuid(),
            driver.Id,
            vehicle.Id,
            rankStopId == Guid.Empty ? null : rankStopId,
            startedAtUtc);

        shift.RaiseDomainEvent(new ShiftOpenedDomainEvent(
            shift.Id,
            shift.DriverId,
            shift.VehicleId,
            Guid.NewGuid(),
            startedAtUtc));

        return shift;
    }

    /// <summary>
    /// Check out by a person (driver, dispatcher) or automatically by the
    /// stale-shift job.
    /// </summary>
    public Result CheckOut(DateTime endedAtUtc, ShiftClosedBy closedBy)
    {
        if (!IsOpen)
        {
            return Result.Failure(ShiftErrors.AlreadyClosed);
        }

        if (endedAtUtc < StartedAt)
        {
            return Result.Failure(ShiftErrors.EndBeforeStart);
        }

        if (closedBy == ShiftClosedBy.Auto && endedAtUtc - StartedAt <= TimeSpan.Zero)
        {
            return Result.Failure(ShiftErrors.EndBeforeStart);
        }

        EndedAt = endedAtUtc;
        ClosedBy = closedBy;

        RaiseDomainEvent(new ShiftClosedDomainEvent(
            Id,
            DriverId,
            VehicleId,
            closedBy,
            Guid.NewGuid(),
            endedAtUtc));

        return Result.Success();
    }
}

public enum ShiftClosedBy
{
    User = 1,
    Auto = 2,
}
