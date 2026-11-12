namespace kecas.application.bookings.ListBookings;

/// <summary>
/// GET /bookings (FR-17): the staff request board, with filters. PassengerId
/// scopes a passenger (or a driver checking) to their own bookings.
/// </summary>
public sealed record ListBookingsQuery(
    int Page,
    int PageSize,
    BookingStatus? Status,
    Guid? PassengerId,
    Guid? TripId) : IQuery<ListBookingsResponse>;
