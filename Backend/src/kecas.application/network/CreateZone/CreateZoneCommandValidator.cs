namespace kecas.application.network.CreateZone;

using FluentValidation;

internal sealed class CreateZoneCommandValidator : AbstractValidator<CreateZoneCommand>
{
    public CreateZoneCommandValidator()
    {
        RuleFor(c => c.Code)
            .NotEmpty().WithMessage("A zone code is required.")
            .MaximumLength(10);

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("A zone name is required.")
            .MaximumLength(120);
    }
}
