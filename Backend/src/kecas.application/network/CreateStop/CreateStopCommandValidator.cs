namespace kecas.application.network.CreateStop;

using FluentValidation;

internal sealed class CreateStopCommandValidator : AbstractValidator<CreateStopCommand>
{
    public CreateStopCommandValidator()
    {
        RuleFor(c => c.Code)
            .NotEmpty().WithMessage("A stop code is required.")
            .MaximumLength(10);

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("A stop name is required.")
            .MaximumLength(120);

        RuleFor(c => c.ZoneId)
            .NotEmpty().WithMessage("A zone is required.");
    }
}
