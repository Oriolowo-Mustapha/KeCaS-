namespace kecas.application.feedback.ListFeedback;

public sealed record FeedbackListItem(
    Guid Id,
    Guid? TripId,
    FeedbackKind Kind,
    string Message,
    int? Rating,
    string? Contact,
    string Status,
    DateTime CreatedAtUtc);

public sealed record ListFeedbackResponse(IReadOnlyList<FeedbackListItem> Items, int Page, int PageSize, int Total);
