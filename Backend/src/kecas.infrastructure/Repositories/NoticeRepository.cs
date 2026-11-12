namespace kecas.infrastructure.Repositories;

using kecas.domain.notices;
using Microsoft.EntityFrameworkCore;

internal sealed class NoticeRepository(ApplicationDbContext context) : INoticeRepository
{
    public Task<Notice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Notices.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Notice>> ListAsync(
        int page,
        int pageSize,
        NoticeStatus? status = null,
        NoticeCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        var notices = await Filter(status, category)
            .AsNoTracking()
            .OrderByDescending(n => n.PublishAtUtc)
            .ThenBy(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return notices;
    }

    public Task<int> CountAsync(
        NoticeStatus? status = null,
        NoticeCategory? category = null,
        CancellationToken cancellationToken = default) =>
        Filter(status, category).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Notice>> ListExpiredPublishedAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var notices = await context.Notices
            .Where(n => n.Status == NoticeStatus.Published
                && n.ExpiresAtUtc != null
                && n.ExpiresAtUtc <= nowUtc)
            .ToListAsync(cancellationToken);

        return notices;
    }

    public async Task<IReadOnlyList<Notice>> ListActiveAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var notices = await context.Notices
            .AsNoTracking()
            .Where(n => n.Status == NoticeStatus.Published
                && n.PublishAtUtc <= nowUtc
                && (n.ExpiresAtUtc == null || n.ExpiresAtUtc > nowUtc))
            .OrderByDescending(n => n.PublishAtUtc)
            .ThenBy(n => n.Id)
            .ToListAsync(cancellationToken);

        return notices;
    }

    public void Add(Notice notice) => context.Notices.Add(notice);

    private IQueryable<Notice> Filter(NoticeStatus? status, NoticeCategory? category)
    {
        var query = context.Notices.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(n => n.Status == status.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(n => n.Category == category.Value);
        }

        return query;
    }
}
