namespace kecas.application.fleet.ChangeVehicleStatus;

using FluentValidation;

internal sealed class ChangeVehicleStatusCommandValidator : AbstractValidator<ChangeVehicleStatusCommand>
{
    public ChangeVehicleStatusCommandValidator()
    {
        RuleFor(c => c.ActorId)
            .NotEmpty();

        RuleFor(c => c.VehicleId)
            .NotEmpty();

        RuleFor(c => c.Status)
            .IsInEnum();

        RuleFor(c => c.Reason)
            .NotEmpty()
            .MaximumLength(300);
    }
}
