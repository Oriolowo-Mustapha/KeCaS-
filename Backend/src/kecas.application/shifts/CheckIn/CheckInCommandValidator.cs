namespace kecas.application.shifts.CheckIn;

using FluentValidation;

internal sealed class CheckInCommandValidator : AbstractValidator<CheckInCommand>
{
    public CheckInCommandValidator()
    {
        RuleFor(c => c.VehicleId)
            .NotEmpty().WithMessage("A vehicle is required to check in.");
    }
}
