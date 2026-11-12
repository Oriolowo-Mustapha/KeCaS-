namespace kecas.domain.fleet;

public static class DriverErrors
{
    public static readonly Error NotFound = new(
        "Driver.NotFound",
        "The specified driver was not found.");

    public static readonly Error OperatorRequired = new(
        "Driver.OperatorRequired",
        "A driver must belong to an operator.");

    public static readonly Error LicenceRequired = new(
        "Driver.LicenceRequired",
        "A licence number is required.");

    public static readonly Error LicenceExpired = new(
        "Driver.LicenceExpired",
        "The licence has already expired.");

    public static readonly Error LicenceInvalid = new(
        "Driver.LicenceInvalid",
        "The driver's licence is invalid or expired.");

    public static readonly Error AlreadySuspended = new(
        "Driver.AlreadySuspended",
        "The driver is already suspended.");

    public static readonly Error AlreadyActive = new(
        "Driver.AlreadyActive",
        "The driver is already active.");

    public static readonly Error AlreadyRetired = new(
        "Driver.AlreadyRetired",
        "The driver is already retired.");

    public static readonly Error AlreadyOnShift = new(
        "Driver.AlreadyOnShift",
        "This driver already has an open shift.");
}
