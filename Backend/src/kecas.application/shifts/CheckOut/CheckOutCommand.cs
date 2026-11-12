namespace kecas.application.shifts.CheckOut;

/// <summary>
/// POST /shifts/:id/check-out (FR-11): close a shift. Drivers may close their
/// own shift; operators and managers (drivers:checkin) may close any.
/// </summary>
public sealed record CheckOutCommand(
    Guid ActorId,
    IReadOnlyList<string> ActorPermissions,
    Guid ShiftId) : ICommand;
