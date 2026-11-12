namespace kecas.domain.common;

/// <summary>
/// A public transport code in the form KK-014 / CB-003 / SH-001, where the
/// two-letter prefix is determined by the vehicle category (design doc §4, BR-01).
/// The category is derivable from the prefix, so a single string column is
/// enough to persist it (FR-04, unique identifier).
/// </summary>
public sealed record VehicleCode
{
    internal VehicleCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public VehicleCategory Category => Value[..2] switch
    {
        "KK" => VehicleCategory.Keke,
        "CB" => VehicleCategory.Cab,
        "SH" => VehicleCategory.Shuttle,
        _ => throw new InvalidOperationException($"Unknown vehicle code prefix: {Value}."),
    };

    private static readonly Dictionary<VehicleCategory, string> Prefixes = new()
    {
        [VehicleCategory.Keke] = "KK",
        [VehicleCategory.Cab] = "CB",
        [VehicleCategory.Shuttle] = "SH",
    };

    public static Result<VehicleCode> Create(string? value, VehicleCategory category)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<VehicleCode>(new Error("VehicleCode.Empty", "A vehicle code is required."));
        }

        var normalized = value.Trim().ToUpperInvariant();

        var prefix = Prefixes[category];

        // Format: prefix, '-', then at least one digit, e.g. KK-014
        var expectedPrefix = $"{prefix}-";
        if (!normalized.StartsWith(expectedPrefix, StringComparison.Ordinal)
            || normalized.Length <= expectedPrefix.Length
            || !normalized[expectedPrefix.Length..].All(char.IsDigit))
        {
            return Result.Failure<VehicleCode>(new Error(
                "VehicleCode.InvalidFormat",
                $"A {category} vehicle code must look like {expectedPrefix}014."));
        }

        return new VehicleCode(normalized);
    }

    public override string ToString() => Value;
}
