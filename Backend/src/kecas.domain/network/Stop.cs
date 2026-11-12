namespace kecas.domain.network;

using kecas.domain.abstractions;

/// <summary>
/// A place vehicles serve (design doc §4.2). A rank is a stop where vehicles
/// wait for passengers.
/// </summary>
public sealed class Stop : Entity
{
    private Stop(Guid id, string code, string name, Guid zoneId, bool isRank)
        : base(id)
    {
        Code = code;
        Name = name;
        ZoneId = zoneId;
        IsRank = isRank;
    }

    private Stop()
    {
        // EF Core
    }

    /// <summary>Stable short code, e.g. S01.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public Guid ZoneId { get; private set; }

    public bool IsRank { get; private set; }

    public static Result<Stop> Create(string code, string name, Guid zoneId, bool isRank = false)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Stop>(NetworkErrors.StopCodeRequired);
        }

        if (code.Trim().Length > 10)
        {
            return Result.Failure<Stop>(NetworkErrors.StopCodeTooLong);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Stop>(NetworkErrors.StopNameRequired);
        }

        if (name.Trim().Length > 120)
        {
            return Result.Failure<Stop>(NetworkErrors.StopNameTooLong);
        }

        if (zoneId == Guid.Empty)
        {
            return Result.Failure<Stop>(NetworkErrors.ZoneRequired);
        }

        return new Stop(Guid.NewGuid(), code.Trim().ToUpperInvariant(), name.Trim(), zoneId, isRank);
    }
}
