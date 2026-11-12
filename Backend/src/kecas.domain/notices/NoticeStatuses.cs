namespace kecas.domain.notices;

/// <summary>
/// Notice lifecycle (FR-22, R2): DRAFT → PUBLISHED → EXPIRED. A nightly job
/// expires notices past their end time (§3.1 scheduled jobs).
/// </summary>
public enum NoticeStatus
{
    Draft = 1,
    Published = 2,
    Expired = 3,
}

public static class NoticeStatusExtensions
{
    /// <summary>Storage values match the design doc CHECK style: DRAFT, ...</summary>
    public static string ToStorageValue(this NoticeStatus status) => status switch
    {
        NoticeStatus.Draft => "DRAFT",
        NoticeStatus.Published => "PUBLISHED",
        NoticeStatus.Expired => "EXPIRED",
        _ => status.ToString().ToUpperInvariant(),
    };

    public static NoticeStatus NoticeStatusFrom(string value) => value switch
    {
        "DRAFT" => NoticeStatus.Draft,
        "PUBLISHED" => NoticeStatus.Published,
        "EXPIRED" => NoticeStatus.Expired,
        _ => Enum.Parse<NoticeStatus>(value, ignoreCase: true),
    };
}
