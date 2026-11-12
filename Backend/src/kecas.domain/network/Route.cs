namespace kecas.domain.network;

using kecas.domain.abstractions;

/// <summary>
/// A named path a vehicle serves in order (design doc §4.2). Fares attach to
/// a route as a whole or to a zone pair within it (§4.2, FR-08).
/// </summary>
public sealed class Route : Entity
{
    private readonly List<RouteStop> _stops = [];

    private Route(
        Guid id,
        string code,
        string name,
        RouteCategory category,
        string? description)
        : base(id)
    {
        Code = code;
        Name = name;
        Category = category;
        Description = description;
        IsActive = true;
    }

    private Route()
    {
        // EF Core
    }

    /// <summary>Stable short code, e.g. KK-1 (Appendix A).</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public RouteCategory Category { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Retired routes stay for history and old fares; they are deactivated, not deleted.</summary>
    public bool IsActive { get; private set; }

    public IReadOnlyList<RouteStop> Stops => _stops.AsReadOnly();

    public static Result<Route> Create(
        string code,
        string name,
        RouteCategory category,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<Route>(NetworkErrors.RouteCodeRequired);
        }

        if (code.Trim().Length > 16)
        {
            return Result.Failure<Route>(NetworkErrors.RouteCodeTooLong);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Route>(NetworkErrors.RouteNameRequired);
        }

        if (name.Trim().Length > 120)
        {
            return Result.Failure<Route>(NetworkErrors.RouteNameTooLong);
        }

        return new Route(
            Guid.NewGuid(),
            code.Trim().ToUpperInvariant(),
            name.Trim(),
            category,
            string.IsNullOrWhiteSpace(description) ? null : description.Trim());
    }

    /// <summary>
    /// Replaces the stop list with the given ordered sequence (BR: every route
    /// has at least one stop; sequence numbers are unique, ascending from 1,
    /// with non-negative minute offsets).
    /// </summary>
    public Result SetStops(IReadOnlyList<(Guid StopId, int Seq, int MinsFromStart)> stops)
    {
        if (stops.Count == 0)
        {
            return Result.Failure(NetworkErrors.RouteStopsRequired);
        }

        // Sequence must be exactly 1..N with no gaps or duplicates.
        for (var i = 0; i < stops.Count; i++)
        {
            if (stops[i].Seq != i + 1)
            {
                return Result.Failure(NetworkErrors.RouteStopSequenceInvalid);
            }

            if (stops[i].MinsFromStart < 0)
            {
                return Result.Failure(NetworkErrors.RouteStopMinutesInvalid);
            }

            if (stops[i].StopId == Guid.Empty)
            {
                return Result.Failure(NetworkErrors.StopNotFound);
            }
        }

        if (stops.Select(s => s.StopId).Distinct().Count() != stops.Count)
        {
            return Result.Failure(NetworkErrors.RouteStopSequenceInvalid);
        }

        _stops.Clear();
        foreach (var (stopId, seq, minsFromStart) in stops)
        {
            _stops.Add(RouteStop.FromSequence(Id, stopId, seq, minsFromStart));
        }

        return Result.Success();
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void Rename(string name)
    {
        Name = name.Trim();
    }

    public void SetDescription(string? description)
    {
        Description = description;
    }
}
