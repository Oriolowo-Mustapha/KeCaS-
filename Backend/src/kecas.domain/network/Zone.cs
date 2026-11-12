namespace kecas.domain.network;

using kecas.domain.abstractions;

/// <summary>
/// A fare area (design doc §4.2: zones are fare areas; campus-only or wider
/// coverage is just more zones [V]). Codes follow Appendix A: Z1..Z5.
/// </summary>
public sealed class Zone : Entity
{
    private Zone(Guid id, string code, string name)
        : base(id)
    {
        Code = code;
        Name = name;
    }

    private Zone()
    {
        // EF Core
    }

    /// <summary>Stable short code, e.g. Z1.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public static Result<Zone> Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Zone>(NetworkErrors.ZoneCodeRequired);
        }

        if (code.Trim().Length > 10)
        {
            return Result.Failure<Zone>(NetworkErrors.ZoneCodeTooLong);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Zone>(NetworkErrors.ZoneNameRequired);
        }

        if (name.Trim().Length > 120)
        {
            return Result.Failure<Zone>(NetworkErrors.ZoneNameTooLong);
        }

        return new Zone(Guid.NewGuid(), code.Trim().ToUpperInvariant(), name.Trim());
    }
}
