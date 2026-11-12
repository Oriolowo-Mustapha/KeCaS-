namespace kecas.domain.notices;

public static class NoticeErrors
{
    public static readonly Error NotFound = new(
        "Notice.NotFound",
        "The specified notice was not found.");

    public static readonly Error TitleInvalid = new(
        "Notice.TitleInvalid",
        "A title of at most 120 characters is required.");

    public static readonly Error BodyInvalid = new(
        "Notice.BodyInvalid",
        "A body of at most 2000 characters is required.");

    public static readonly Error ExpiryBeforePublish = new(
        "Notice.ExpiryBeforePublish",
        "The notice cannot expire before it is published.");

    public static readonly Error CreatorRequired = new(
        "Notice.CreatorRequired",
        "The notice must record who created it.");

    public static readonly Error NotEditable = new(
        "Notice.NotEditable",
        "Only draft notices can be edited.");

    public static readonly Error NotPublishable = new(
        "Notice.NotPublishable",
        "Only draft notices can be published.");

    public static readonly Error NotExpirable = new(
        "Notice.NotExpirable",
        "Only published notices can be expired.");
}

/// <summary>Repository for the Notice aggregate (FR-22).</summary>
public interface INoticeRepository
{
    Task<Notice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notice>> ListAsync(
        int page,
        int pageSize,
        NoticeStatus? status = null,
        NoticeCategory? category = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        NoticeStatus? status = null,
        NoticeCategory? category = null,
        CancellationToken cancellationToken = default);

    /// <summary>Published notices whose end time has passed, for the expiry job.</summary>
    Task<IReadOnlyList<Notice>> ListExpiredPublishedAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Active notices for the public page (cached endpoint).</summary>
    Task<IReadOnlyList<Notice>> ListActiveAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    void Add(Notice notice);
}
