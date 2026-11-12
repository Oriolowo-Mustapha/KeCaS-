namespace kecas.application.trips.CompleteTrip;

using FluentValidation;

internal sealed class CompleteTripCommandValidator : AbstractValidator<CompleteTripCommand>
{
    public CompleteTripCommandValidator()
    {
        RuleFor(c => c.TripId).NotEmpty();
    }
}
