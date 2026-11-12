namespace kecas.application.fares.CreateFare;

using FluentValidation;

internal sealed class CreateFareCommandValidator : AbstractValidator<CreateFareCommand>
{
    public CreateFareCommandValidator()
    {
        RuleFor(c => c.AmountKobo)
            .GreaterThan(0).WithMessage("A fare amount greater than zero is required.");

        RuleFor(c => c.EffectiveFrom)
            .NotEmpty().WithMessage("A fare needs an effective start date.");

        RuleFor(c => c)
            .Must(c => (c.RouteId is { } r && r != Guid.Empty)
                != (c.FromZoneId is { } fz && fz != Guid.Empty && c.ToZoneId is { } tz && tz != Guid.Empty))
            .WithMessage("A fare must target exactly one route, or one from/to zone pair.");

        RuleFor(c => c)
            // Route fares inherit the route's category; zone-pair fares must
            // name the category they price (§6.2).
            .Must(c => c.RouteId is { } r && r != Guid.Empty || c.Category is not null)
            .WithMessage("A zone-pair fare must specify a category (KEKE, CAB or SHUTTLE).");

        RuleFor(c => c)
            .Must(c => c.Category is null || c.RouteId == null || c.RouteId == Guid.Empty)
            .WithMessage("A route fare cannot carry a category; it follows the route.");

        RuleFor(c => c)
            .Must(c => c.FromZoneId == null || c.ToZoneId == null || c.FromZoneId != c.ToZoneId)
            .WithMessage("The from and to zones must differ.");
    }
}
