namespace kecas.application.payments.ListPayments;

public sealed record PaymentListItem(
    Guid Id,
    Guid? BookingId,
    Guid? TripId,
    int AmountKobo,
    PaymentMethod Method,
    string Status,
    string? Reference,
    DateTime RecordedAtUtc);

public sealed record ListPaymentsResponse(IReadOnlyList<PaymentListItem> Items, int Page, int PageSize, int Total);
