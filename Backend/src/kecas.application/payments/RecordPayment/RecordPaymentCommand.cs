namespace kecas.application.payments.RecordPayment;

/// <summary>
/// POST /payments (FR-18, DD-08): staff record a payment that happened outside
/// the system (cash, transfer, POS). No gateway — records only. Exactly one of
/// BookingId / TripId must be set.
/// </summary>
public sealed record RecordPaymentCommand(
    Guid ActorId,
    Guid? BookingId,
    Guid? TripId,
    int AmountKobo,
    PaymentMethod Method,
    PaymentStatus Status,
    string? Reference) : ICommand<Guid>;
