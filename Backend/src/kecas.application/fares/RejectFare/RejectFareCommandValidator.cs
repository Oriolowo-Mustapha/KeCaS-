namespace kecas.application.fares.RejectFare;

using FluentValidation;

internal sealed class RejectFareCommandValidator : AbstractValidator<RejectFareCommand>
{
    public RejectFareCommandValidator()
    {
        RuleFor(c => c.FareId).NotEmpty();
    }
}
