namespace kecas.application.notices.ListNotices;

public sealed record NoticeListItem(
    Guid Id,
    string Title,
    string Body,
    NoticeAudience Audience,
    NoticeCategory Category,
    DateTime PublishAtUtc,
    DateTime? ExpiresAtUtc,
    string Status,
    DateTime CreatedAtUtc);

public sealed record ListNoticesResponse(IReadOnlyList<NoticeListItem> Items, int Page, int PageSize, int Total);
