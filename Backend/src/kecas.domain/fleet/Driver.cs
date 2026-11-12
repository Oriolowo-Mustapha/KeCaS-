namespace kecas.domain.fleet;

using kecas.domain.abstractions;

/// <summary>
/// A driver registered in the system, optionally linked to a signed-in user
/// account (user_id FK optional — not every driver carries a smartphone,
/// design doc §5.1). Licence numbers are encrypted at rest in Infrastructure
/// and masked in API responses (FR-06).
/// </summary>
public sealed class Driver : Entity
{
    private Driver(
        Guid id,
        Guid? userId,
        Guid operatorId,
        string licenceNumber,
        DateOnly licenceExpiry,
        DriverStatus status)
        : base(id)
    {
        UserId = userId;
        OperatorId = operatorId;
        LicenceNumber = licenceNumber;
        LicenceExpiry = licenceExpiry;
        Status = status;
    }

    private Driver()
    {
        // EF Core
    }

    /// <summary>Optional signed-in account for the driver view.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>The operator that employs this driver ("own org" scoping).</summary>
    public Guid OperatorId { get; private set; }

    public string LicenceNumber { get; private set; } = string.Empty;

    public DateOnly LicenceExpiry { get; private set; }

    public DriverStatus Status { get; private set; }

    public static Result<Driver> Create(
        Guid? userId,
        Guid operatorId,
        string licenceNumber,
        DateOnly licenceExpiry)
    {
        if (operatorId == Guid.Empty)
        {
            return Result.Failure<Driver>(DriverErrors.OperatorRequired);
        }

        if (string.IsNullOrWhiteSpace(licenceNumber))
        {
            return Result.Failure<Driver>(DriverErrors.LicenceRequired);
        }

        if (licenceExpiry <= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return Result.Failure<Driver>(DriverErrors.LicenceExpired);
        }

        return new Driver(
            Guid.NewGuid(),
            userId == Guid.Empty ? null : userId,
            operatorId,
            licenceNumber.Trim(),
            licenceExpiry,
            DriverStatus.Active);
    }

    /// <summary>
    /// The check-in guard from design doc §7.2: a driver must be Active with a
    /// valid licence date before opening a shift.
    /// </summary>
    public bool CanCheckIn(DateOnly asOfDate) =>
        Status == DriverStatus.Active && LicenceExpiry >= asOfDate;

    public Result UpdateLicence(string licenceNumber, DateOnly licenceExpiry)
    {
        if (string.IsNullOrWhiteSpace(licenceNumber))
        {
            return Result.Failure<Driver>(DriverErrors.LicenceRequired);
        }

        LicenceNumber = licenceNumber.Trim();
        LicenceExpiry = licenceExpiry;
        return Result.Success();
    }

    public Result Suspend()
    {
        if (Status == DriverStatus.Suspended)
        {
            return Result.Failure(DriverErrors.AlreadySuspended);
        }

        Status = DriverStatus.Suspended;
        return Result.Success();
    }

    public Result Reinstate()
    {
        if (Status == DriverStatus.Active)
        {
            return Result.Failure(DriverErrors.AlreadyActive);
        }

        Status = DriverStatus.Active;
        return Result.Success();
    }

    public Result Retire()
    {
        if (Status == DriverStatus.Retired)
        {
            return Result.Failure(DriverErrors.AlreadyRetired);
        }

        Status = DriverStatus.Retired;
        return Result.Success();
    }
}
