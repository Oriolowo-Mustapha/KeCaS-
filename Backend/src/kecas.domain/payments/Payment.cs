namespace kecas.domain.payments;

using kecas.domain.abstractions;
using kecas.domain.common;

/// <summary>
/// A payment RECORD only (DD-08, FR-18): amount, method, status, reference.
/// Money moves outside the system (cash, transfer, POS) — no gateway, no card
/// data, ever.
/// </summary>
public sealed class Payment : Entity
{
    private Payment(
        Guid id,
        Guid? bookingId,
        Guid? tripId,
        Money amount,
        PaymentMethod method,
        PaymentStatus status,
        string? reference,
        Guid? recordedBy,
        DateTime recordedAtUtc)
        : base(id)
    {
        BookingId = bookingId;
        TripId = tripId;
        Amount = amount;
        Method = method;
        Status = status;
        Reference = reference;
        RecordedBy = recordedBy;
        RecordedAtUtc = recordedAtUtc;
    }

    private Payment()
    {
        // EF Core
    }

    /// <summary>Exactly one of BookingId / TripId must be set (§4.2).</summary>
    public Guid? BookingId { get; private set; }

    public Guid? TripId { get; private set; }

    public Money Amount { get; private set; }

    public PaymentMethod Method { get; private set; }

    public PaymentStatus Status { get; private set; }

    public string? Reference { get; private set; }

    public Guid? RecordedBy { get; private set; }

    public DateTime RecordedAtUtc { get; private set; }

    public static Result<Payment> Create(
        Guid? bookingId,
        Guid? tripId,
        Money amount,
        PaymentMethod method,
        PaymentStatus status,
        string? reference,
        Guid? recordedBy,
        DateTime recordedAtUtc)
    {
        var hasBooking = bookingId is { } b && b != Guid.Empty;
        var hasTrip = tripId is { } t && t != Guid.Empty;
        if (hasBooking == hasTrip)
        {
            return Result.Failure<Payment>(PaymentErrors.ScopeInvalid);
        }

        if (amount == Money.Zero || amount.AmountInKobo <= 0)
        {
            return Result.Failure<Payment>(PaymentErrors.AmountInvalid);
        }

        if (reference is { } r && r.Trim().Length > 100)
        {
            return Result.Failure<Payment>(PaymentErrors.ReferenceInvalid);
        }

        return new Payment(
            Guid.NewGuid(),
            hasBooking ? bookingId : null,
            hasTrip ? tripId : null,
            amount,
            method,
            status,
            string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            recordedBy is { } by && by != Guid.Empty ? by : null,
            recordedAtUtc);
    }

    /// <summary>Staff correct the record's status later (e.g. PENDING → SUCCESSFUL).</summary>
    public Result UpdateStatus(PaymentStatus status)
    {
        if (Status == status)
        {
            return Result.Success();
        }

        Status = status;
        return Result.Success();
    }
}
