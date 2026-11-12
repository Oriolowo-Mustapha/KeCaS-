namespace kecas.domain.fleet;

public static class ShiftErrors
{
    public static readonly Error NotFound = new(
        "Shift.NotFound",
        "The specified shift was not found.");

    public static readonly Error AlreadyClosed = new(
        "Shift.AlreadyClosed",
        "This shift is already closed.");

    public static readonly Error EndBeforeStart = new(
        "Shift.EndBeforeStart",
        "A shift cannot end before it started.");

    public static readonly Error VehicleAlreadyOnShift = new(
        "Shift.VehicleAlreadyOnShift",
        "This vehicle already has an open shift.");

    public static readonly Error AlreadyOpen = new(
        "Shift.AlreadyOpen",
        "This driver already has an open shift.");
}
