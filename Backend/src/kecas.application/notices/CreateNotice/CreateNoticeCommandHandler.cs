namespace kecas.application.notices.CreateNotice;

using System.Text.Json;
using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;
using kecas.domain.notices;

internal sealed class CreateNoticeCommandHandler(
    INoticeRepository noticeRepository,
    IDateTimeProvider dateTimeProvider,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateNoticeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateNoticeCommand command, CancellationToken cancellationToken)
    {
        var publishAt = command.PublishAtUtc == default
            ? dateTimeProvider.UtcNow
            : command.PublishAtUtc;

        var created = Notice.Create(
            command.Title,
            command.Body,
            command.Audience,
            command.Category,
            publishAt,
            command.ExpiresAtUtc,
            command.ActorId,
            dateTimeProvider.UtcNow);
        if (created.IsFailure)
        {
            return Result.Failure<Guid>(created.Error);
        }

        noticeRepository.Add(created.Value);

        auditWriter.Append(
            command.ActorId,
            "notice.created",
            "notice",
            created.Value.Id.ToString(),
            after: JsonSerializer.Serialize(new
            {
                title = created.Value.Title,
                audience = created.Value.Audience.ToStorageValue(),
                category = created.Value.Category.ToStorageValue(),
                status = created.Value.Status.ToStorageValue(),
            }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(created.Value.Id);
    }
}
