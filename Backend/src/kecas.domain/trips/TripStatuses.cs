namespace kecas.domain.trips;

/// <summary>
/// Trip lifecycle (design doc §5.2): SCHEDULED → IN_PROGRESS → COMPLETED /
/// CANCELLED. Walk-up keke and cab runs logged after the fact are created
/// directly as COMPLETED.
/// </summary>
public enum TripStatus
{
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
}

/// <summary>Where a trip record came from (design doc §4.2).</summary>
public enum TripSource
{
    Logged = 1,
    Scheduled = 2,
    Booking = 3,
}

/// <summary>
/// Storage values match the design doc and the database CHECK constraints:
/// SCHEDULED / IN_PROGRESS / COMPLETED / CANCELLED, LOGGED / SCHEDULED /
/// BOOKING. Snake case for the multi-word state (same rule as fares).
/// </summary>
public static class TripStatusExtensions
{
    public static string ToStorageValue(this TripStatus status) => status switch
    {
        TripStatus.InProgress => "IN_PROGRESS",
        _ => status.ToString().ToUpperInvariant(),
    };
}

public static class TripSourceExtensions
{
    public static string ToStorageValue(this TripSource source) =>
        source.ToString().ToUpperInvariant();
}
