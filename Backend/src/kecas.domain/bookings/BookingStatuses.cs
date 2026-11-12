namespace kecas.domain.bookings;

/// <summary>
/// Booking lifecycle (§5.2/§5.3): REQUESTED → CONFIRMED → COMPLETED, or
/// REJECTED / CANCELLED / NO_SHOW. Confirmation is request-and-confirm
/// (DD-09): a manager confirms, and BR-09 capacity is checked in a
/// transaction before the status changes.
/// </summary>
public enum BookingStatus
{
    Requested = 1,
    Confirmed = 2,
    Completed = 3,
    Rejected = 4,
    Cancelled = 5,
    NoShow = 6,
}

public static class BookingStatusExtensions
{
    /// <summary>Storage values match §5.2: REQUESTED, NO_SHOW, ...</summary>
    public static string ToStorageValue(this BookingStatus status) => status switch
    {
        BookingStatus.Requested => "REQUESTED",
        BookingStatus.Confirmed => "CONFIRMED",
        BookingStatus.Completed => "COMPLETED",
        BookingStatus.Rejected => "REJECTED",
        BookingStatus.Cancelled => "CANCELLED",
        BookingStatus.NoShow => "NO_SHOW",
        _ => status.ToString().ToUpperInvariant(),
    };

    public static BookingStatus BookingStatusFrom(string value) => value switch
    {
        "REQUESTED" => BookingStatus.Requested,
        "CONFIRMED" => BookingStatus.Confirmed,
        "COMPLETED" => BookingStatus.Completed,
        "REJECTED" => BookingStatus.Rejected,
        "CANCELLED" => BookingStatus.Cancelled,
        "NO_SHOW" => BookingStatus.NoShow,
        _ => Enum.Parse<BookingStatus>(value, ignoreCase: true),
    };
}
