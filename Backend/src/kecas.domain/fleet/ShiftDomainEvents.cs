namespace kecas.domain.fleet;

/// <summary>
/// Raised when a driver checks in. An open shift contributes to the vehicle
/// evaluating as "Available" in the derived availability rules (design doc §5.1).
/// </summary>
public sealed record ShiftOpenedDomainEvent(
    Guid ShiftId,
    Guid DriverId,
    Guid VehicleId,
    Guid Id,
    DateTime OccurredAtUtc) : IDomainEvent;

/// <summary>
/// Raised when a shift is closed (by user or by the nightly stale-shift job).
/// </summary>
public sealed record ShiftClosedDomainEvent(
    Guid ShiftId,
    Guid DriverId,
    Guid VehicleId,
    ShiftClosedBy ClosedBy,
    Guid Id,
    DateTime OccurredAtUtc) : IDomainEvent;
