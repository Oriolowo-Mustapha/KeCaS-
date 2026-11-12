namespace kecas.application.notices.PublishNotice;

using System.Text.Json;
using kecas.application.abstractions.data;
using kecas.domain.notices;

internal sealed class PublishNoticeCommandHandler(
    INoticeRepository noticeRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<PublishNoticeCommand>
{
    public async Task<Result> Handle(PublishNoticeCommand command, CancellationToken cancellationToken)
    {
        var notice = await noticeRepository.GetByIdAsync(command.NoticeId, cancellationToken);
        if (notice is null)
        {
            return Result.Failure(NoticeErrors.NotFound);
        }

        var result = notice.Publish();
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "notice.published",
            "notice",
            notice.Id.ToString(),
            before: JsonSerializer.Serialize(new { status = "DRAFT" }),
            after: JsonSerializer.Serialize(new { status = notice.Status.ToStorageValue() }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
