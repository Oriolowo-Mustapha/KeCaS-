namespace kecas.application.notices.ListNotices;

using kecas.domain.notices;

internal sealed class ListNoticesQueryHandler(INoticeRepository noticeRepository)
    : IQueryHandler<ListNoticesQuery, ListNoticesResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<ListNoticesResponse>> Handle(
        ListNoticesQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var notices = await noticeRepository.ListAsync(
            page, pageSize, query.Status, query.Category, cancellationToken);
        var total = await noticeRepository.CountAsync(
            query.Status, query.Category, cancellationToken);

        var items = notices
            .Select(n => new NoticeListItem(
                n.Id,
                n.Title,
                n.Body,
                n.Audience,
                n.Category,
                n.PublishAtUtc,
                n.ExpiresAtUtc,
                n.Status.ToStorageValue(),
                n.CreatedAtUtc))
            .ToList();

        return Result.Success(new ListNoticesResponse(items, page, pageSize, total));
    }
}
