namespace kecas.domain.abstractions;

public record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");

    public static readonly Error ConditionNotMet = new(
        "Error.ConditionNotMet",
        "The specified condition was not met.");
}
