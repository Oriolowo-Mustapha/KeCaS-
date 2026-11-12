namespace kecas.application.network.CreateRoute;

using FluentValidation;

internal sealed class CreateRouteCommandValidator : AbstractValidator<CreateRouteCommand>
{
    public CreateRouteCommandValidator()
    {
        RuleFor(c => c.Code)
            .NotEmpty().WithMessage("A route code is required.")
            .MaximumLength(16);

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("A route name is required.")
            .MaximumLength(120);

        RuleFor(c => c.Category)
            .IsInEnum().WithMessage("The route category must be KEKE, CAB or SHUTTLE.");

        RuleFor(c => c.Stops)
            .NotEmpty().WithMessage("A route needs at least one stop.");

        RuleForEach(c => c.Stops).ChildRules(stop =>
        {
            stop.RuleFor(s => s.StopId).NotEmpty();
            stop.RuleFor(s => s.MinsFromStart)
                .GreaterThanOrEqualTo(0).WithMessage("Minutes from the start stop cannot be negative.");
        });
    }
}
