namespace kecas.application.notices.ExpireNotice;

using System.Text.Json;
using kecas.application.abstractions.data;
using kecas.domain.notices;

internal sealed class ExpireNoticeCommandHandler(
    INoticeRepository noticeRepository,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ExpireNoticeCommand>
{
    public async Task<Result> Handle(ExpireNoticeCommand command, CancellationToken cancellationToken)
    {
        var notice = await noticeRepository.GetByIdAsync(command.NoticeId, cancellationToken);
        if (notice is null)
        {
            return Result.Failure(NoticeErrors.NotFound);
        }

        var result = notice.Expire();
        if (result.IsFailure)
        {
            return result;
        }

        auditWriter.Append(
            command.ActorId,
            "notice.expired",
            "notice",
            notice.Id.ToString(),
            before: JsonSerializer.Serialize(new { status = "PUBLISHED" }),
            after: JsonSerializer.Serialize(new { status = notice.Status.ToStorageValue() }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
