namespace kecas.api.Endpoints;

/// <summary>
/// Query-string enum filters arrive in the design doc's storage casing
/// (KEKE, PUBLISHED, ...). Minimal API binding is name-exact, so parse
/// case-insensitively here instead.
/// </summary>
internal static class EnumQueryParser
{
    public static T? Parse<T>(string? value) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<T>(value.Trim(), ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }
}
