namespace kecas.application.feedback.SubmitFeedback;

using System.Text.Json;
using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;
using kecas.domain.feedback;
using kecas.domain.trips;

internal sealed class SubmitFeedbackCommandHandler(
    IFeedbackRepository feedbackRepository,
    ITripRepository tripRepository,
    IDateTimeProvider dateTimeProvider,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SubmitFeedbackCommand, Guid>
{
    public async Task<Result<Guid>> Handle(SubmitFeedbackCommand command, CancellationToken cancellationToken)
    {
        if (command.TripId is { } tripId
            && await tripRepository.GetByIdAsync(tripId, cancellationToken) is null)
        {
            return Result.Failure<Guid>(TripErrors.NotFound);
        }

        var created = Feedback.Create(
            command.TripId,
            command.Kind,
            command.Message,
            command.Rating,
            command.Contact,
            dateTimeProvider.UtcNow);
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        feedbackRepository.Add(created.Value);

        // System action: the submitter is anonymous (FR-21).
        auditWriter.Append(
            actorId: null,
            "feedback.submitted",
            "feedback",
            created.Value.Id.ToString(),
            after: JsonSerializer.Serialize(new
            {
                kind = created.Value.Kind.ToStorageValue(),
                tripId = created.Value.TripId,
                rating = created.Value.Rating,
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(created.Value.Id);
    }
}
