namespace kecas.application.notices.GetPublicNotices;

public sealed record PublicNoticeItem(
    Guid Id,
    string Title,
    string Body,
    NoticeCategory Category,
    DateTime PublishAtUtc,
    DateTime? ExpiresAtUtc);

public sealed record PublicNoticesResponse(DateTime AsOfUtc, IReadOnlyList<PublicNoticeItem> Items);
