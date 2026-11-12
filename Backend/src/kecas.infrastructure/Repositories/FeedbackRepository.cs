namespace kecas.infrastructure.Repositories;

using kecas.domain.feedback;
using Microsoft.EntityFrameworkCore;

internal sealed class FeedbackRepository(ApplicationDbContext context) : IFeedbackRepository
{
    public Task<Feedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Feedback.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Feedback>> ListAsync(
        int page,
        int pageSize,
        FeedbackStatus? status = null,
        FeedbackKind? kind = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Feedback.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        if (kind.HasValue)
        {
            query = query.Where(f => f.Kind == kind.Value);
        }

        var feedback = await query
            .AsNoTracking()
            .OrderByDescending(f => f.CreatedAtUtc)
            .ThenBy(f => f.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return feedback;
    }

    public async Task<int> CountAsync(
        FeedbackStatus? status = null,
        FeedbackKind? kind = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Feedback.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        if (kind.HasValue)
        {
            query = query.Where(f => f.Kind == kind.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public void Add(Feedback feedback) => context.Feedback.Add(feedback);
}
