namespace kecas.domain.feedback;

using kecas.domain.abstractions;

/// <summary>
/// Passenger feedback or complaints about a trip or the service (FR-21).
/// Minimal collection: a kind, a message, an optional rating and an optional
/// contact — no account or personal data required.
/// </summary>
public sealed class Feedback : Entity
{
    private Feedback(
        Guid id,
        Guid? tripId,
        FeedbackKind kind,
        string message,
        int? rating,
        string? contact,
        FeedbackStatus status,
        DateTime createdAtUtc)
        : base(id)
    {
        TripId = tripId;
        Kind = kind;
        Message = message;
        Rating = rating;
        Contact = contact;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    private Feedback()
    {
        // EF Core
    }

    public Guid? TripId { get; private set; }

    public FeedbackKind Kind { get; private set; }

    public string Message { get; private set; } = null!;

    public int? Rating { get; private set; }

    public string? Contact { get; private set; }

    public FeedbackStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Result<Feedback> Create(
        Guid? tripId,
        FeedbackKind kind,
        string? message,
        int? rating,
        string? contact,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(message) || message.Trim().Length > 1000)
        {
            return Result.Failure<Feedback>(FeedbackErrors.MessageInvalid);
        }

        if (rating is { } r && (r < 1 || r > 5))
        {
            return Result.Failure<Feedback>(FeedbackErrors.RatingInvalid);
        }

        if (contact is { } c && c.Trim().Length > 120)
        {
            return Result.Failure<Feedback>(FeedbackErrors.ContactInvalid);
        }

        return new Feedback(
            Guid.NewGuid(),
            tripId is { } t && t != Guid.Empty ? t : null,
            kind,
            message.Trim(),
            rating,
            string.IsNullOrWhiteSpace(contact) ? null : contact.Trim(),
            FeedbackStatus.New,
            createdAtUtc);
    }

    /// <summary>Manager triage: NEW → REVIEWED → RESOLVED.</summary>
    public Result Review(Guid reviewerId, FeedbackStatus target)
    {
        if (target is not (FeedbackStatus.Reviewed or FeedbackStatus.Resolved))
        {
            return Result.Failure(FeedbackErrors.StatusInvalid);
        }

        if (Status == FeedbackStatus.Resolved)
        {
            return Result.Failure(FeedbackErrors.StatusInvalid);
        }

        Status = target;
        return Result.Success();
    }
}
