namespace kecas.application.notices.ExpireNotice;

/// <summary>POST /notices/:id/expire (FR-22): take a notice off the board.</summary>
public sealed record ExpireNoticeCommand(Guid ActorId, Guid NoticeId) : ICommand;
