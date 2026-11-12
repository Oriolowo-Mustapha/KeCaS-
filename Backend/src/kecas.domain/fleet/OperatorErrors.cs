namespace kecas.domain.fleet;

public static class OperatorErrors
{
    public static readonly Error NotFound = new(
        "Operator.NotFound",
        "The specified operator was not found.");

    public static readonly Error NameRequired = new(
        "Operator.NameRequired",
        "An operator name is required.");

    public static readonly Error NameTooLong = new(
        "Operator.NameTooLong",
        "The operator name may not exceed 120 characters.");

    public static readonly Error KindInvalid = new(
        "Operator.KindInvalid",
        "The operator kind is not valid.");

    public static readonly Error AlreadyInactive = new(
        "Operator.AlreadyInactive",
        "The operator is already inactive.");

    public static readonly Error AlreadyActive = new(
        "Operator.AlreadyActive",
        "The operator is already active.");
}
