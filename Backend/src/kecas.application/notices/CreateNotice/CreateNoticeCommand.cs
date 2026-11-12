namespace kecas.application.notices.CreateNotice;

/// <summary>
/// POST /notices (FR-22): create a DRAFT notice. Publishing is a separate
/// action so nothing reaches the public site by accident.
/// </summary>
public sealed record CreateNoticeCommand(
    Guid ActorId,
    string Title,
    string Body,
    NoticeAudience Audience,
    NoticeCategory Category,
    DateTime PublishAtUtc,
    DateTime? ExpiresAtUtc) : ICommand<Guid>;
