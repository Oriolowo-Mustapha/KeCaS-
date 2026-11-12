namespace kecas.application.trips.UpdateTrip;

using FluentValidation;

internal sealed class UpdateTripCommandValidator : AbstractValidator<UpdateTripCommand>
{
    public UpdateTripCommandValidator()
    {
        RuleFor(c => c.TripId).NotEmpty();

        RuleFor(c => c)
            .Must(c => c.StartsAtUtc is not null || c.EndsAtUtc is not null
                || c.RouteId is not null || c.VehicleId is not null || c.DriverId is not null)
            .WithMessage("At least one field must be provided.");

        RuleFor(c => c.EndsAtUtc)
            .GreaterThan(c => c.StartsAtUtc ?? DateTime.MinValue)
            .When(c => c.EndsAtUtc is not null && c.StartsAtUtc is not null)
            .WithMessage("A trip cannot end before it starts.");
    }
}
