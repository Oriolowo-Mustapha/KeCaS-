namespace kecas.domain.payments;

public static class PaymentErrors
{
    public static readonly Error NotFound = new(
        "Payment.NotFound",
        "The specified payment record was not found.");

    public static readonly Error ScopeInvalid = new(
        "Payment.ScopeInvalid",
        "A payment record must reference a booking or a trip, not both.");

    public static readonly Error AmountInvalid = new(
        "Payment.AmountInvalid",
        "The payment amount must be greater than zero.");

    public static readonly Error ReferenceInvalid = new(
        "Payment.ReferenceInvalid",
        "The reference must be at most 100 characters.");

    public static readonly Error BookingNotFound = new(
        "Payment.BookingNotFound",
        "The specified booking was not found.");

    public static readonly Error TripNotFound = new(
        "Payment.TripNotFound",
        "The specified trip was not found.");
}

/// <summary>Repository for the Payment aggregate (FR-18).</summary>
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> ListAsync(
        int page,
        int pageSize,
        PaymentStatus? status = null,
        Guid? bookingId = null,
        Guid? tripId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        PaymentStatus? status = null,
        Guid? bookingId = null,
        Guid? tripId = null,
        CancellationToken cancellationToken = default);

    void Add(Payment payment);
}
