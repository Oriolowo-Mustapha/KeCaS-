namespace kecas.application.payments.ListPayments;

/// <summary>GET /payments (FR-18): the payment ledger with filters.</summary>
public sealed record ListPaymentsQuery(
    int Page,
    int PageSize,
    PaymentStatus? Status,
    Guid? BookingId,
    Guid? TripId) : IQuery<ListPaymentsResponse>;
