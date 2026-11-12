namespace kecas.application.fleet.UpdateDriver;

using FluentValidation;

internal sealed class UpdateDriverCommandValidator : AbstractValidator<UpdateDriverCommand>
{
    public UpdateDriverCommandValidator()
    {
        RuleFor(c => c.ActorId)
            .NotEmpty();

        RuleFor(c => c.DriverId)
            .NotEmpty();

        RuleFor(c => c.LicenceNumber)
            .NotEmpty()
            .MaximumLength(50)
            .When(c => c.LicenceNumber is not null);

        RuleFor(c => c.Status)
            .IsInEnum()
            .When(c => c.Status.HasValue);
    }
}
