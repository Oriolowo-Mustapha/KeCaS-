namespace kecas.domain.common;

/// <summary>
/// A Nigerian plate number, stored upper-case with no spaces (design doc §4.2).
/// </summary>
public sealed record PlateNumber
{
    internal PlateNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<PlateNumber> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<PlateNumber>(new Error("PlateNumber.Empty", "A plate number is required."));
        }

        var normalized = value.Trim().ToUpperInvariant().Replace(" ", string.Empty);

        if (normalized.Length is < 5 or > 12)
        {
            return Result.Failure<PlateNumber>(new Error(
                "PlateNumber.InvalidLength",
                "A plate number must be between 5 and 12 characters."));
        }

        if (!normalized.All(char.IsLetterOrDigit) || !normalized.Any(char.IsLetter))
        {
            return Result.Failure<PlateNumber>(new Error(
                "PlateNumber.InvalidCharacters",
                "A plate number may only contain letters and digits."));
        }

        return new PlateNumber(normalized);
    }

    public override string ToString() => Value;
}
