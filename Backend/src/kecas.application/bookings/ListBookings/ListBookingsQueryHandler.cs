namespace kecas.application.bookings.ListBookings;

using kecas.domain.bookings;

internal sealed class ListBookingsQueryHandler(IBookingRepository bookingRepository)
    : IQueryHandler<ListBookingsQuery, ListBookingsResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<ListBookingsResponse>> Handle(
        ListBookingsQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var bookings = await bookingRepository.ListAsync(
            page, pageSize, query.Status, query.PassengerId, query.TripId, cancellationToken);
        var total = await bookingRepository.CountAsync(
            query.Status, query.PassengerId, query.TripId, cancellationToken);

        var items = bookings
            .Select(b => new BookingListItem(
                b.Id,
                b.BookingCode,
                b.PassengerId,
                b.TripId,
                b.PickupStopId,
                b.DropoffStopId,
                b.PartySize,
                b.Status.ToStorageValue(),
                b.RideCode,
                b.DecisionReason,
                b.CreatedAtUtc))
            .ToList();

        return Result.Success(new ListBookingsResponse(items, page, pageSize, total));
    }
}
