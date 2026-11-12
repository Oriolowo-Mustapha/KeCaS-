namespace kecas.application.trips.CancelTrip;

using FluentValidation;

internal sealed class CancelTripCommandValidator : AbstractValidator<CancelTripCommand>
{
    public CancelTripCommandValidator()
    {
        RuleFor(c => c.TripId).NotEmpty();

        RuleFor(c => c.Reason)
            .NotEmpty().WithMessage("A reason is required to cancel a trip.");
    }
}
