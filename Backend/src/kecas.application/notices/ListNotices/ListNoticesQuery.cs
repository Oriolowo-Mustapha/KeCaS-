namespace kecas.application.notices.ListNotices;

/// <summary>GET /notices (FR-22): staff notice board with filters.</summary>
public sealed record ListNoticesQuery(
    int Page,
    int PageSize,
    NoticeStatus? Status,
    NoticeCategory? Category) : IQuery<ListNoticesResponse>;
