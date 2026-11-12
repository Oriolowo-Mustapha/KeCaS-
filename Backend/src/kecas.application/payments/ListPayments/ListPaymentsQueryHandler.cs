namespace kecas.application.payments.ListPayments;

using kecas.domain.payments;

internal sealed class ListPaymentsQueryHandler(IPaymentRepository paymentRepository)
    : IQueryHandler<ListPaymentsQuery, ListPaymentsResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<ListPaymentsResponse>> Handle(
        ListPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var payments = await paymentRepository.ListAsync(
            page, pageSize, query.Status, query.BookingId, query.TripId, cancellationToken);
        var total = await paymentRepository.CountAsync(
            query.Status, query.BookingId, query.TripId, cancellationToken);

        var items = payments
            .Select(p => new PaymentListItem(
                p.Id,
                p.BookingId,
                p.TripId,
                p.Amount.AmountInKobo,
                p.Method,
                p.Status.ToStorageValue(),
                p.Reference,
                p.RecordedAtUtc))
            .ToList();

        return Result.Success(new ListPaymentsResponse(items, page, pageSize, total));
    }
}
