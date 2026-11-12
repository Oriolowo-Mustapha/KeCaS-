namespace kecas.application.notices.GetPublicNotices;

using kecas.application.abstractions.clock;
using kecas.domain.notices;

internal sealed class GetPublicNoticesQueryHandler(
    INoticeRepository noticeRepository,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetPublicNoticesQuery, PublicNoticesResponse>
{
    public async Task<Result<PublicNoticesResponse>> Handle(
        GetPublicNoticesQuery query,
        CancellationToken cancellationToken)
    {
        var nowUtc = dateTimeProvider.UtcNow;

        var active = await noticeRepository.ListActiveAsync(nowUtc, cancellationToken);

        // NFR-05: the public site shows passenger-facing notices only.
        var items = active
            .Where(n => n.Audience is NoticeAudience.Everyone or NoticeAudience.Passengers)
            .Select(n => new PublicNoticeItem(
                n.Id,
                n.Title,
                n.Body,
                n.Category,
                n.PublishAtUtc,
                n.ExpiresAtUtc))
            .ToList();

        return Result.Success(new PublicNoticesResponse(nowUtc, items));
    }
}
