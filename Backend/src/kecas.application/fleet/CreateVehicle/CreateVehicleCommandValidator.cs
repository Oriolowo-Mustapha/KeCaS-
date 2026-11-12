namespace kecas.application.fleet.CreateVehicle;

using FluentValidation;

internal sealed class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(c => c.Code)
            .NotEmpty()
            .MaximumLength(16)
            .Must(code => code.Contains('-'))
            .WithMessage("A vehicle code must include its category prefix, e.g. KK-014.");

        RuleFor(c => c.PlateNumber)
            .NotEmpty()
            .MaximumLength(12);

        RuleFor(c => c.Capacity)
            .InclusiveBetween(1, 60);

        RuleFor(c => c.OperatorId)
            .NotEmpty();
    }
}
