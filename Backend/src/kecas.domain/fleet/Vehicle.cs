namespace kecas.domain.fleet;

using kecas.domain.abstractions;
using kecas.domain.common;

/// <summary>
/// A registered keke, cab or shuttle. BR-01: every vehicle has a unique
/// system identifier (VehicleCode) and unique plate. BR-02: a vehicle that is
/// not Active must not be assigned to a new trip.
/// </summary>
public sealed class Vehicle : Entity
{
    private Vehicle(
        Guid id,
        VehicleCode code,
        PlateNumber plateNumber,
        VehicleCategory category,
        int capacity,
        Guid operatorId,
        VehicleStatus status)
        : base(id)
    {
        Code = code;
        PlateNumber = plateNumber;
        Category = category;
        Capacity = capacity;
        OperatorId = operatorId;
        Status = status;
        StatusChangedAt = DateTime.UtcNow;
    }

    private Vehicle()
    {
        // EF Core
    }

    /// <summary>Unique public code, e.g. KK-014 (BR-01).</summary>
    public VehicleCode Code { get; private set; } = null!;

    /// <summary>Unique plate number, upper-case with no spaces.</summary>
    public PlateNumber PlateNumber { get; private set; } = null!;

    public VehicleCategory Category { get; private set; }

    public int Capacity { get; private set; }

    /// <summary>The operator that owns this vehicle ("own org" scoping).</summary>
    public Guid OperatorId { get; private set; }

    public VehicleStatus Status { get; private set; }

    public DateTime StatusChangedAt { get; private set; }

    /// <summary>Concurrency token for two-session edits (design doc §4.2).</summary>
    public long RowVersion { get; private set; }

    public static Result<Vehicle> Create(
        string code,
        string plateNumber,
        VehicleCategory category,
        int capacity,
        Guid operatorId)
    {
        var codeResult = VehicleCode.Create(code, category);
        if (!codeResult.IsSuccess)
        {
            return Result.Failure<Vehicle>(codeResult.Error);
        }

        var plateResult = PlateNumber.Create(plateNumber);
        if (!plateResult.IsSuccess)
        {
            return Result.Failure<Vehicle>(plateResult.Error);
        }

        if (capacity is < 1 or > 60)
        {
            return Result.Failure<Vehicle>(VehicleErrors.CapacityInvalid);
        }

        if (operatorId == Guid.Empty)
        {
            return Result.Failure<Vehicle>(VehicleErrors.OperatorRequired);
        }

        return new Vehicle(
            Guid.NewGuid(),
            codeResult.Value,
            plateResult.Value,
            category,
            capacity,
            operatorId,
            VehicleStatus.Active);
    }

    /// <summary>
    /// BR-02: only Active vehicles may take a new trip or shift.
    /// </summary>
    public bool CanBeAssigned => Status == VehicleStatus.Active;

    /// <summary>
    /// Park the vehicle (Inactive) or send it for service (Maintenance).
    /// A reason is always required (design doc §5.3 transition rules).
    /// </summary>
    public Result ChangeStatus(VehicleStatus newStatus, string reason)
    {
        if (Status == VehicleStatus.Retired)
        {
            return Result.Failure(VehicleErrors.RetiredCannotChange);
        }

        if (newStatus == Status)
        {
            return Result.Failure(VehicleErrors.SameStatus);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(VehicleErrors.ReasonRequired);
        }

        if (!Enum.IsDefined(newStatus))
        {
            return Result.Failure(VehicleErrors.StatusInvalid);
        }

        var previous = Status;
        Status = newStatus;
        StatusChangedAt = DateTime.UtcNow;
        RowVersion++;

        RaiseDomainEvent(new VehicleStatusChangedDomainEvent(
            Id,
            previous,
            newStatus,
            reason,
            Guid.NewGuid(),
            DateTime.UtcNow));

        return Result.Success();
    }

    public Result UpdateCapacity(int capacity)
    {
        if (capacity is < 1 or > 60)
        {
            return Result.Failure(VehicleErrors.CapacityInvalid);
        }

        Capacity = capacity;
        return Result.Success();
    }
}
