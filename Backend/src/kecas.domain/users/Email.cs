namespace kecas.domain.users;

/// <summary>
/// A normalised e-mail address value object. Normalisation is lowercase trim,
/// which keeps the uniqueness check forgiving (design doc §4.2: email unique).
/// </summary>
public sealed record Email
{
    internal Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Email>(new Error("Email.Empty", "An email address is required."));
        }

        var normalized = value.Trim().ToLowerInvariant();

        // Minimal shape check: one '@', non-empty local part and domain.
        var at = normalized.IndexOf('@');
        if (at <= 0 || at == normalized.Length - 1 || normalized.Contains(' '))
        {
            return Result.Failure<Email>(new Error("Email.InvalidFormat", "The email address is not valid."));
        }

        return new Email(normalized);
    }

    public override string ToString() => Value;
}
