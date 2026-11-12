namespace kecas.domain.feedback;

public enum FeedbackKind
{
    Complaint = 1,
    Suggestion = 2,
    Praise = 3,
}

public enum FeedbackStatus
{
    New = 1,
    Reviewed = 2,
    Resolved = 3,
}

public static class FeedbackEnums
{
    public static string ToStorageValue(this FeedbackKind kind) => kind switch
    {
        FeedbackKind.Complaint => "COMPLAINT",
        FeedbackKind.Suggestion => "SUGGESTION",
        FeedbackKind.Praise => "PRAISE",
        _ => kind.ToString().ToUpperInvariant(),
    };

    public static FeedbackKind FeedbackKindFrom(string value) => value switch
    {
        "COMPLAINT" => FeedbackKind.Complaint,
        "SUGGESTION" => FeedbackKind.Suggestion,
        "PRAISE" => FeedbackKind.Praise,
        _ => Enum.Parse<FeedbackKind>(value, ignoreCase: true),
    };

    public static string ToStorageValue(this FeedbackStatus status) => status switch
    {
        FeedbackStatus.New => "NEW",
        FeedbackStatus.Reviewed => "REVIEWED",
        FeedbackStatus.Resolved => "RESOLVED",
        _ => status.ToString().ToUpperInvariant(),
    };

    public static FeedbackStatus FeedbackStatusFrom(string value) => value switch
    {
        "NEW" => FeedbackStatus.New,
        "REVIEWED" => FeedbackStatus.Reviewed,
        "RESOLVED" => FeedbackStatus.Resolved,
        _ => Enum.Parse<FeedbackStatus>(value, ignoreCase: true),
    };
}

public static class FeedbackErrors
{
    public static readonly Error NotFound = new(
        "Feedback.NotFound",
        "The specified feedback was not found.");

    public static readonly Error MessageInvalid = new(
        "Feedback.MessageInvalid",
        "A message of at most 1000 characters is required.");

    public static readonly Error RatingInvalid = new(
        "Feedback.RatingInvalid",
        "The rating must be between 1 and 5.");

    public static readonly Error ContactInvalid = new(
        "Feedback.ContactInvalid",
        "The contact must be at most 120 characters.");

    public static readonly Error StatusInvalid = new(
        "Feedback.StatusInvalid",
        "Only NEW or REVIEWED feedback can move to REVIEWED or RESOLVED.");
}

/// <summary>Repository for the Feedback aggregate (FR-21).</summary>
public interface IFeedbackRepository
{
    Task<Feedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Feedback>> ListAsync(
        int page,
        int pageSize,
        FeedbackStatus? status = null,
        FeedbackKind? kind = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        FeedbackStatus? status = null,
        FeedbackKind? kind = null,
        CancellationToken cancellationToken = default);

    void Add(Feedback feedback);
}
