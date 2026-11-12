namespace kecas.application.feedback.ListFeedback;

using kecas.domain.feedback;

internal sealed class ListFeedbackQueryHandler(IFeedbackRepository feedbackRepository)
    : IQueryHandler<ListFeedbackQuery, ListFeedbackResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<ListFeedbackResponse>> Handle(
        ListFeedbackQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var feedback = await feedbackRepository.ListAsync(
            page, pageSize, query.Status, query.Kind, cancellationToken);
        var total = await feedbackRepository.CountAsync(
            query.Status, query.Kind, cancellationToken);

        var items = feedback
            .Select(f => new FeedbackListItem(
                f.Id,
                f.TripId,
                f.Kind,
                f.Message,
                f.Rating,
                f.Contact,
                f.Status.ToStorageValue(),
                f.CreatedAtUtc))
            .ToList();

        return Result.Success(new ListFeedbackResponse(items, page, pageSize, total));
    }
}
