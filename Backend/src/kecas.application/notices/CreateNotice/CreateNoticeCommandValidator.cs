namespace kecas.application.notices.CreateNotice;

using FluentValidation;

internal sealed class CreateNoticeCommandValidator : AbstractValidator<CreateNoticeCommand>
{
    public CreateNoticeCommandValidator()
    {
        RuleFor(c => c.ActorId).NotEmpty();

        RuleFor(c => c.Title)
            .NotEmpty().WithMessage("A title is required.")
            .MaximumLength(120);

        RuleFor(c => c.Body)
            .NotEmpty().WithMessage("A body is required.")
            .MaximumLength(2000);

        RuleFor(c => c.Audience).IsInEnum();

        RuleFor(c => c.Category).IsInEnum();
    }
}
