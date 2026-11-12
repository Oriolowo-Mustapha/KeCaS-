namespace kecas.application.trips.CreateTrip;

using FluentValidation;
using kecas.domain.trips;

internal sealed class CreateTripCommandValidator : AbstractValidator<CreateTripCommand>
{
    public CreateTripCommandValidator()
    {
        RuleFor(c => c.RouteId).NotEmpty().WithMessage("A route is required (BR-04).");
        RuleFor(c => c.VehicleId).NotEmpty().WithMessage("A vehicle is required.");
        RuleFor(c => c.DriverId).NotEmpty().WithMessage("A driver is required.");

        RuleFor(c => c.Source)
            .IsInEnum().WithMessage("The trip source must be LOGGED, SCHEDULED or BOOKING.");

        RuleFor(c => c.EndsAtUtc)
            .GreaterThan(c => c.StartsAtUtc)
            .WithMessage("A trip cannot end before it starts.");
    }
}
