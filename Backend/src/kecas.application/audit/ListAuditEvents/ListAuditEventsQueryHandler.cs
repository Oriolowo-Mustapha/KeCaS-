namespace kecas.application.audit.ListAuditEvents;

using kecas.application.abstractions.data;

public sealed record ListAuditEventsResponse(
    IReadOnlyList<AuditEventListItem> Items,
    int Page,
    int PageSize,
    int Total);

internal sealed class ListAuditEventsQueryHandler(IAuditReader auditReader)
    : IQueryHandler<ListAuditEventsQuery, ListAuditEventsResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<ListAuditEventsResponse>> Handle(
        ListAuditEventsQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var (items, total) = await auditReader.ListAsync(
            page,
            pageSize,
            query.ActorId,
            string.IsNullOrWhiteSpace(query.EntityType) ? null : query.EntityType.Trim().ToLowerInvariant(),
            query.FromUtc,
            query.ToUtc,
            cancellationToken);

        return Result.Success(new ListAuditEventsResponse(items, page, pageSize, total));
    }
}
