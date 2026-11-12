namespace kecas.application.feedback.SubmitFeedback;

/// <summary>
/// POST /feedback (FR-21): anonymous feedback — no session required, minimal
/// collection (message, optional trip reference, rating, contact).
/// </summary>
public sealed record SubmitFeedbackCommand(
    Guid? TripId,
    FeedbackKind Kind,
    string Message,
    int? Rating,
    string? Contact) : ICommand<Guid>;
