namespace kecas.application.public_.GetPublicAvailability;

/// <summary>
/// GET /public/availability?category=&amp;stop= (FR-11, NFR-05): counts by
/// category plus as_of — no individual driver data.
/// </summary>
public sealed record GetPublicAvailabilityQuery(string? Category, string? StopCode)
    : IQuery<PublicAvailabilityResponse>;
