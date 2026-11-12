namespace kecas.infrastructure.Repositories;

using kecas.domain.bookings;
using Microsoft.EntityFrameworkCore;

internal sealed class BookingRepository(ApplicationDbContext context) : IBookingRepository
{
    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Booking?> GetByCodeAsync(string bookingCode, CancellationToken cancellationToken = default) =>
        context.Bookings.FirstOrDefaultAsync(b => b.BookingCode == bookingCode.ToUpperInvariant(), cancellationToken);

    public async Task<IReadOnlyList<Booking>> ListAsync(
        int page,
        int pageSize,
        BookingStatus? status = null,
        Guid? passengerId = null,
        Guid? tripId = null,
        CancellationToken cancellationToken = default)
    {
        var bookings = await Filter(status, passengerId, tripId)
            .AsNoTracking()
            .OrderByDescending(b => b.CreatedAtUtc)
            .ThenBy(b => b.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return bookings;
    }

    public Task<int> CountAsync(
        BookingStatus? status = null,
        Guid? passengerId = null,
        Guid? tripId = null,
        CancellationToken cancellationToken = default) =>
        Filter(status, passengerId, tripId).CountAsync(cancellationToken);

    public async Task<int> SumConfirmedPartySizeAsync(
        Guid tripId,
        CancellationToken cancellationToken = default)
    {
        // BR-09: read INSIDE the confirm transaction, after the trip row lock.
        var sum = await context.Bookings
            .Where(b => b.TripId == tripId && b.Status == BookingStatus.Confirmed)
            .SumAsync(b => (int?)b.PartySize, cancellationToken);

        return sum ?? 0;
    }

    public void Add(Booking booking) => context.Bookings.Add(booking);

    private IQueryable<Booking> Filter(
        BookingStatus? status,
        Guid? passengerId,
        Guid? tripId)
    {
        var query = context.Bookings.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(b => b.Status == status.Value);
        }

        if (passengerId.HasValue)
        {
            query = query.Where(b => b.PassengerId == passengerId.Value);
        }

        if (tripId.HasValue)
        {
            query = query.Where(b => b.TripId == tripId.Value);
        }

        return query;
    }
}
