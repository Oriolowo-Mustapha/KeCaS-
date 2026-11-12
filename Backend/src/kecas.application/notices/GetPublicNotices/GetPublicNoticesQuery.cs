namespace kecas.application.notices.GetPublicNotices;

/// <summary>
/// GET /public/notices (FR-22): currently active notices for the public site.
/// Audience PASSENGERS/EVERYONE only — driver/staff notices stay internal.
/// </summary>
public sealed record GetPublicNoticesQuery : IQuery<PublicNoticesResponse>;
