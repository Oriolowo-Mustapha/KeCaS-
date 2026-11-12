namespace kecas.domain.notices;

/// <summary>
/// A service notice shown on the public site (FR-22): schedule, route or
/// service changes. Created as a DRAFT by a manager, then published; the
/// nightly job (or a manual hide) moves published notices to EXPIRED.
/// </summary>
public sealed class Notice : Entity
{
    private Notice(
        Guid id,
        string title,
        string body,
        NoticeAudience audience,
        NoticeCategory category,
        DateTime publishAtUtc,
        DateTime? expiresAtUtc,
        Guid createdBy,
        NoticeStatus status,
        DateTime createdAtUtc)
        : base(id)
    {
        Title = title;
        Body = body;
        Audience = audience;
        Category = category;
        PublishAtUtc = publishAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        CreatedBy = createdBy;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    private Notice()
    {
        // EF Core
    }

    public string Title { get; private set; } = null!;

    public string Body { get; private set; } = null!;

    public NoticeAudience Audience { get; private set; }

    public NoticeCategory Category { get; private set; }

    /// <summary>When the notice becomes visible (defaults to now).</summary>
    public DateTime PublishAtUtc { get; private set; }

    /// <summary>When the notice stops being shown; null = until manually hidden.</summary>
    public DateTime? ExpiresAtUtc { get; private set; }

    public Guid CreatedBy { get; private set; }

    public NoticeStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Result<Notice> Create(
        string? title,
        string? body,
        NoticeAudience audience,
        NoticeCategory category,
        DateTime publishAtUtc,
        DateTime? expiresAtUtc,
        Guid createdBy,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 120)
        {
            return Result.Failure<Notice>(NoticeErrors.TitleInvalid);
        }

        if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > 2000)
        {
            return Result.Failure<Notice>(NoticeErrors.BodyInvalid);
        }

        if (expiresAtUtc is { } expires && expires <= publishAtUtc)
        {
            return Result.Failure<Notice>(NoticeErrors.ExpiryBeforePublish);
        }

        if (createdBy == Guid.Empty)
        {
            return Result.Failure<Notice>(NoticeErrors.CreatorRequired);
        }

        return new Notice(
            Guid.NewGuid(),
            title.Trim(),
            body.Trim(),
            audience,
            category,
            publishAtUtc,
            expiresAtUtc,
            createdBy,
            NoticeStatus.Draft,
            createdAtUtc);
    }

    /// <summary>Only drafts may be edited (the published text is what riders see).</summary>
    public Result Edit(
        string? title,
        string? body,
        NoticeAudience audience,
        NoticeCategory category,
        DateTime? publishAtUtc,
        DateTime? expiresAtUtc)
    {
        if (Status != NoticeStatus.Draft)
        {
            return Result.Failure(NoticeErrors.NotEditable);
        }

        if (title is { } t && (!string.IsNullOrWhiteSpace(t) && t.Trim().Length <= 120))
        {
            Title = t.Trim();
        }
        else if (title is not null)
        {
            return Result.Failure(NoticeErrors.TitleInvalid);
        }

        if (body is { } b)
        {
            if (string.IsNullOrWhiteSpace(b) || b.Trim().Length > 2000)
            {
                return Result.Failure(NoticeErrors.BodyInvalid);
            }

            Body = b.Trim();
        }

        Audience = audience;
        Category = category;

        if (publishAtUtc is { } publish)
        {
            PublishAtUtc = publish;
        }

        if (expiresAtUtc is { } expires)
        {
            if (expires <= PublishAtUtc)
            {
                return Result.Failure(NoticeErrors.ExpiryBeforePublish);
            }

            ExpiresAtUtc = expires;
        }

        return Result.Success();
    }

    public Result Publish()
    {
        if (Status != NoticeStatus.Draft)
        {
            return Result.Failure(NoticeErrors.NotPublishable);
        }

        Status = NoticeStatus.Published;
        return Result.Success();
    }

    /// <summary>Called by a manager (hide) or the nightly expiry job.</summary>
    public Result Expire()
    {
        if (Status != NoticeStatus.Published)
        {
            return Result.Failure(NoticeErrors.NotExpirable);
        }

        Status = NoticeStatus.Expired;
        return Result.Success();
    }

    /// <summary>Visible to the public right now (§6.2 active notices).</summary>
    public bool IsActive(DateTime nowUtc) =>
        Status == NoticeStatus.Published
        && PublishAtUtc <= nowUtc
        && (ExpiresAtUtc is null || ExpiresAtUtc > nowUtc);
}
