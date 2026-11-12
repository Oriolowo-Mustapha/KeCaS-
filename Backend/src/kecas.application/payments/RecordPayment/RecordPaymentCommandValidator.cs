namespace kecas.application.payments.RecordPayment;

using FluentValidation;

internal sealed class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(c => c.ActorId).NotEmpty();

        RuleFor(c => c.AmountKobo).GreaterThan(0);

        RuleFor(c => c.Method).IsInEnum();

        RuleFor(c => c.Status).IsInEnum();

        RuleFor(c => c.Reference)
            .MaximumLength(100)
            .When(c => c.Reference is not null);

        // Exactly one of BookingId / TripId.
        RuleFor(c => c)
            .Must(c => (c.BookingId is not null) ^ (c.TripId is not null))
            .WithMessage("A payment record must reference a booking or a trip, not both.");
    }
}
