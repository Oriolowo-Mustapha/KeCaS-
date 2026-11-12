namespace kecas.application.notices.UpdateNotice;

using System.Text.Json;
using kecas.application.abstractions.data;
using kecas.domain.notices;

internal sealed class UpdateNoticeCommandHandler(
    INoticeRepository noticeRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateNoticeCommand>
{
    public async Task<Result> Handle(UpdateNoticeCommand command, CancellationToken cancellationToken)
    {
        var notice = await noticeRepository.GetByIdAsync(command.NoticeId, cancellationToken);
        if (notice is null)
        {
            return Result.Failure(NoticeErrors.NotFound);
        }

        var before = JsonSerializer.Serialize(new
        {
            title = notice.Title,
            audience = notice.Audience.ToStorageValue(),
            category = notice.Category.ToStorageValue(),
        });

        var result = notice.Edit(
            command.Title,
            command.Body,
            command.Audience ?? notice.Audience,
            command.Category ?? notice.Category,
            command.PublishAtUtc,
            command.ExpiresAtUtc);
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "notice.updated",
            "notice",
            notice.Id.ToString(),
            before: before,
            after: JsonSerializer.Serialize(new
            {
                title = notice.Title,
                audience = notice.Audience.ToStorageValue(),
                category = notice.Category.ToStorageValue(),
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
