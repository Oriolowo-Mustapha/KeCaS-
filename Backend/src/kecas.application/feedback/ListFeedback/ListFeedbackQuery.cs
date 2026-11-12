namespace kecas.application.feedback.ListFeedback;

/// <summary>GET /feedback (FR-21): staff triage board with filters.</summary>
public sealed record ListFeedbackQuery(
    int Page,
    int PageSize,
    FeedbackStatus? Status,
    FeedbackKind? Kind) : IQuery<ListFeedbackResponse>;
