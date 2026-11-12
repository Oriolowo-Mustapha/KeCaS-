namespace kecas.application.notices.PublishNotice;

/// <summary>POST /notices/:id/publish (FR-22): DRAFT → PUBLISHED.</summary>
public sealed record PublishNoticeCommand(Guid ActorId, Guid NoticeId) : ICommand;
