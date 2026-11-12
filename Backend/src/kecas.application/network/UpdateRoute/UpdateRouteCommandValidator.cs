namespace kecas.application.network.UpdateRoute;

using FluentValidation;

internal sealed class UpdateRouteCommandValidator : AbstractValidator<UpdateRouteCommand>
{
    public UpdateRouteCommandValidator()
    {
        RuleFor(c => c.RouteId).NotEmpty();

        RuleFor(c => c.Name)
            .MaximumLength(120)
            .When(c => c.Name is not null);

        RuleFor(c => c)
            .Must(c => c.Name is not null || c.Description is not null || c.IsActive is not null)
            .WithMessage("At least one field must be provided.");
    }
}
