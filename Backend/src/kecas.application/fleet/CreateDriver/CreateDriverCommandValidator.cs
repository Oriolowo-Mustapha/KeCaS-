namespace kecas.application.fleet.CreateDriver;

using FluentValidation;

internal sealed class CreateDriverCommandValidator : AbstractValidator<CreateDriverCommand>
{
    public CreateDriverCommandValidator()
    {
        RuleFor(c => c.ActorId)
            .NotEmpty();

        RuleFor(c => c.OperatorId)
            .NotEmpty();

        RuleFor(c => c.LicenceNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(c => c.LicenceExpiry)
            .NotEmpty();
    }
}
