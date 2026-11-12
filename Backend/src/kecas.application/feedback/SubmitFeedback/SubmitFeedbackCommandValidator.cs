namespace kecas.application.feedback.SubmitFeedback;

using FluentValidation;

internal sealed class SubmitFeedbackCommandValidator : AbstractValidator<SubmitFeedbackCommand>
{
    public SubmitFeedbackCommandValidator()
    {
        RuleFor(c => c.Kind).IsInEnum();

        RuleFor(c => c.Message)
            .NotEmpty().WithMessage("A message is required.")
            .MaximumLength(1000);

        RuleFor(c => c.Rating)
            .InclusiveBetween(1, 5)
            .When(c => c.Rating is not null);

        RuleFor(c => c.Contact)
            .MaximumLength(120)
            .When(c => c.Contact is not null);
    }
}
