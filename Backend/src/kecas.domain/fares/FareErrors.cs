namespace kecas.domain.fares;

public static class FareErrors
{
    public static readonly Error AmountInvalid = new(
        "Fare.AmountInvalid",
        "A fare amount greater than zero is required.");

    public static readonly Error ScopeInvalid = new(
        "Fare.ScopeInvalid",
        "A fare must target exactly one route, or one from/to zone pair.");

    public static readonly Error ZonePairInvalid = new(
        "Fare.ZonePairInvalid",
        "The from and to zones of a zone-pair fare must differ.");

    public static readonly Error EffectiveFromInvalid = new(
        "Fare.EffectiveFromInvalid",
        "A fare needs an effective start date.");

    public static readonly Error EffectiveToBeforeFrom = new(
        "Fare.EffectiveToBeforeFrom",
        "The effective end date cannot be before the start date.");

    public static readonly Error NotFound = new(
        "Fare.NotFound",
        "The specified fare was not found.");

    public static readonly Error NotEditable = new(
        "Fare.NotEditable",
        "Only draft fares can be edited.");

    public static readonly Error NotSubmittable = new(
        "Fare.NotSubmittable",
        "Only draft fares can be submitted for approval.");

    public static readonly Error NotApprovable = new(
        "Fare.NotApprovable",
        "Only fares pending approval can be approved or rejected.");

    public static readonly Error SelfApproval = new(
        "Fare.SelfApproval",
        "The approver cannot be the user who proposed the fare.");

    public static readonly Error NotSupersedable = new(
        "Fare.NotSupersedable",
        "Only published fares can be superseded.");
}
