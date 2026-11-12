namespace kecas.domain.fleet;

public static class VehicleErrors
{
    public static readonly Error NotFound = new(
        "Vehicle.NotFound",
        "The specified vehicle was not found.");

    public static readonly Error PlateDuplicate = new(
        "Vehicle.PlateDuplicate",
        "A vehicle with this plate number is already registered.");

    public static readonly Error CodeDuplicate = new(
        "Vehicle.CodeDuplicate",
        "A vehicle with this vehicle code is already registered.");

    public static readonly Error CapacityInvalid = new(
        "Vehicle.CapacityInvalid",
        "Vehicle capacity must be between 1 and 60.");

    public static readonly Error OperatorRequired = new(
        "Vehicle.OperatorRequired",
        "A vehicle must belong to an operator.");

    public static readonly Error RetiredCannotChange = new(
        "Vehicle.RetiredCannotChange",
        "A retired vehicle cannot change status.");

    public static readonly Error SameStatus = new(
        "Vehicle.SameStatus",
        "The vehicle is already in that status.");

    public static readonly Error ReasonRequired = new(
        "Vehicle.ReasonRequired",
        "A reason is required for a status change.");

    public static readonly Error StatusInvalid = new(
        "Vehicle.StatusInvalid",
        "The requested vehicle status is not valid.");

    public static readonly Error NotAssignable = new(
        "Vehicle.NotAssignable",
        "This vehicle cannot be assigned because it is not active.");
}
