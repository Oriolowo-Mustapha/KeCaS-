namespace kecas.application.notices.UpdateNotice;

/// <summary>
/// PATCH /notices/:id (FR-22): edit a DRAFT notice. Published notices are
/// frozen; hide them with /expire instead.
/// </summary>
public sealed record UpdateNoticeCommand(
    Guid ActorId,
    Guid NoticeId,
    string? Title,
    string? Body,
    NoticeAudience? Audience,
    NoticeCategory? Category,
    DateTime? PublishAtUtc,
    DateTime? ExpiresAtUtc) : ICommand;
