namespace kecas.application.fares.ApproveFare;

using FluentValidation;

internal sealed class ApproveFareCommandValidator : AbstractValidator<ApproveFareCommand>
{
    public ApproveFareCommandValidator()
    {
        RuleFor(c => c.FareId).NotEmpty();
    }
}
