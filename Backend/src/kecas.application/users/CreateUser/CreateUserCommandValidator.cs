namespace kecas.application.users.CreateUser;

using FluentValidation;

internal sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(c => c.ActorId)
            .NotEmpty();

        RuleFor(c => c.FullName)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(c => c.Phone)
            .MaximumLength(20)
            .When(c => !string.IsNullOrWhiteSpace(c.Phone));

        RuleFor(c => c.Password)
            .NotEmpty()
            .MinimumLength(10)
            .Matches("[A-Za-z]").WithMessage("The password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("The password must contain at least one digit.");

        RuleFor(c => c.Roles)
            .NotEmpty()
            .WithMessage("At least one role is required.");
    }
}
