namespace kecas.application.shifts.CheckIn;

/// <summary>
/// POST /shifts/check-in (FR-11, §7.2): open a shift with a vehicle (and an
/// optional rank). Drivers check in for themselves; operators and managers
/// (drivers:checkin) may check a driver in on their behalf by passing DriverId.
/// </summary>
public sealed record CheckInCommand(
    Guid ActorId,
    IReadOnlyList<string> ActorPermissions,
    Guid? DriverId,
    Guid VehicleId,
    Guid? RankStopId) : ICommand<Guid>;
