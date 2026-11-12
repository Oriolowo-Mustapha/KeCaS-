namespace kecas.application.users.UpdateUser;

using FluentValidation;

internal sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(c => c.ActorId)
            .NotEmpty();

        RuleFor(c => c.UserId)
            .NotEmpty();

        RuleFor(c => c.FullName)
            .NotEmpty()
            .MaximumLength(120)
            .When(c => c.FullName is not null);

        RuleFor(c => c.Phone)
            .MaximumLength(20)
            .When(c => !string.IsNullOrWhiteSpace(c.Phone));

        RuleFor(c => c.Roles)
            .NotEmpty()
            .WithMessage("When roles are supplied, at least one role is required.")
            .When(c => c.Roles is not null);
    }
}
