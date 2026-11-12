namespace kecas.application.fares.SubmitFare;

using FluentValidation;

internal sealed class SubmitFareCommandValidator : AbstractValidator<SubmitFareCommand>
{
    public SubmitFareCommandValidator()
    {
        RuleFor(c => c.FareId).NotEmpty();
    }
}
