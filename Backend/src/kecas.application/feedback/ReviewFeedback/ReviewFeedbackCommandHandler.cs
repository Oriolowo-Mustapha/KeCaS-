namespace kecas.application.feedback.ReviewFeedback;

using System.Text.Json;
using kecas.application.abstractions.data;
using kecas.domain.feedback;

internal sealed class ReviewFeedbackCommandHandler(
    IFeedbackRepository feedbackRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReviewFeedbackCommand>
{
    public async Task<Result> Handle(ReviewFeedbackCommand command, CancellationToken cancellationToken)
    {
        var feedback = await feedbackRepository.GetByIdAsync(command.FeedbackId, cancellationToken);
        if (feedback is null)
        {
            return Result.Failure(FeedbackErrors.NotFound);
        }

        var before = feedback.Status.ToStorageValue();

        var result = feedback.Review(command.ActorId, command.Target);
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "feedback.reviewed",
            "feedback",
            feedback.Id.ToString(),
            before: JsonSerializer.Serialize(new { status = before }),
            after: JsonSerializer.Serialize(new { status = feedback.Status.ToStorageValue() }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
