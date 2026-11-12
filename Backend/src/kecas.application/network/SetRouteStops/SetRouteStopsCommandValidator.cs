namespace kecas.application.network.SetRouteStops;

using FluentValidation;
using kecas.application.network.CreateRoute;

internal sealed class SetRouteStopsCommandValidator : AbstractValidator<SetRouteStopsCommand>
{
    public SetRouteStopsCommandValidator()
    {
        RuleFor(c => c.RouteId).NotEmpty();

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
