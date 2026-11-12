namespace kecas.application.bookings.ListBookings;

public sealed record BookingListItem(
    Guid Id,
    string BookingCode,
    Guid PassengerId,
    Guid TripId,
    Guid? PickupStopId,
    Guid? DropoffStopId,
    int PartySize,
    string Status,
    string? RideCode,
    string? DecisionReason,
    DateTime CreatedAtUtc);

public sealed record ListBookingsResponse(IReadOnlyList<BookingListItem> Items, int Page, int PageSize, int Total);
