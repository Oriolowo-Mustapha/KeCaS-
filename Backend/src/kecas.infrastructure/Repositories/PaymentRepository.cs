namespace kecas.infrastructure.Repositories;

using kecas.domain.payments;
using Microsoft.EntityFrameworkCore;

internal sealed class PaymentRepository(ApplicationDbContext context) : IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Payment>> ListAsync(
        int page,
        int pageSize,
        PaymentStatus? status = null,
        Guid? bookingId = null,
        Guid? tripId = null,
        CancellationToken cancellationToken = default)
    {
        var payments = await Filter(status, bookingId, tripId)
            .AsNoTracking()
            .OrderByDescending(p => p.RecordedAtUtc)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return payments;
    }

    public Task<int> CountAsync(
        PaymentStatus? status = null,
        Guid? bookingId = null,
        Guid? tripId = null,
        CancellationToken cancellationToken = default) =>
        Filter(status, bookingId, tripId).CountAsync(cancellationToken);

    public void Add(Payment payment) => context.Payments.Add(payment);

    private IQueryable<Payment> Filter(
        PaymentStatus? status,
        Guid? bookingId,
        Guid? tripId)
    {
        var query = context.Payments.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (bookingId.HasValue)
        {
            query = query.Where(p => p.BookingId == bookingId.Value);
        }

        if (tripId.HasValue)
        {
            query = query.Where(p => p.TripId == tripId.Value);
        }

        return query;
    }
}
