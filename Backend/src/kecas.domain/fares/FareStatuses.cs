namespace kecas.domain.fares;

/// <summary>
/// Fare lifecycle states (design doc §5.2): DRAFT → PENDING_APPROVAL →
/// PUBLISHED, with REJECTED and SUPERSEDED as terminal exits. Only PUBLISHED
/// fares are visible on public endpoints.
/// </summary>
public enum FareStatus
{
    Draft = 1,
    PendingApproval = 2,
    Published = 3,
    Rejected = 4,
    Superseded = 5,
}

/// <summary>
/// Storage values match the design doc §5.2 and the database CHECK
/// constraint: DRAFT, PENDING_APPROVAL, PUBLISHED, REJECTED, SUPERSEDED.
/// </summary>
public static class FareStatusExtensions
{
    public static string ToStorageValue(this FareStatus status) => status switch
    {
        // Snake case for the multi-word state: PENDING_APPROVAL.
        FareStatus.PendingApproval => "PENDING_APPROVAL",
        _ => status.ToString().ToUpperInvariant(),
    };
}
