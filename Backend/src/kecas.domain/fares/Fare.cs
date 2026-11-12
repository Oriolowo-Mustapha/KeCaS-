namespace kecas.domain.fares;

using kecas.domain.abstractions;
using kecas.domain.common;
using kecas.domain.network;

/// <summary>
/// A price for transport, scoped to a whole route OR a from/to zone pair
/// (§4.2, FR-08). Lifecycle per §5.2/§5.3:
/// DRAFT → PENDING_APPROVAL → PUBLISHED (approve ≠ proposer) / REJECTED,
/// and a PUBLISHED fare is superseded by the newer published fare for the
/// same scope. Amounts are whole kobo. Dates are inclusive; only PUBLISHED
/// fares are publicly visible.
/// </summary>
public sealed class Fare : Entity
{
    private Fare(
        Guid id,
        Guid? routeId,
        RouteCategory? category,
        Guid? fromZoneId,
        Guid? toZoneId,
        Money amount,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        Guid proposedBy,
        FareStatus status)
        : base(id)
    {
        RouteId = routeId;
        Category = category;
        FromZoneId = fromZoneId;
        ToZoneId = toZoneId;
        Amount = amount;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        ProposedBy = proposedBy;
        Status = status;
    }

    private Fare()
    {
        // EF Core
    }

    /// <summary>Set when the fare covers a whole route; null for zone-pair fares.</summary>
    public Guid? RouteId { get; private set; }

    /// <summary>
    /// Vehicle category the fare applies to. Null for route fares (the route
    /// fixes it); required for zone-pair fares (§6.2: fares carry a category).
    /// </summary>
    public RouteCategory? Category { get; private set; }

    /// <summary>Set when the fare covers a zone pair; null for route fares.</summary>
    public Guid? FromZoneId { get; private set; }

    public Guid? ToZoneId { get; private set; }

    public Money Amount { get; private set; }

    public DateOnly EffectiveFrom { get; private set; }

    /// <summary>Inclusive end date; null = open-ended until superseded.</summary>
    public DateOnly? EffectiveTo { get; private set; }

    public Guid ProposedBy { get; private set; }

    /// <summary>Set on approval; must differ from ProposedBy (§5.3).</summary>
    public Guid? ApprovedBy { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public FareStatus Status { get; private set; }

    /// <summary>The published fare this one replaced, when created via supersede.</summary>
    public Guid? SupersedesId { get; private set; }

    /// <summary>
    /// Records which published fare this one replaces. Called by the approve
    /// handler so supersede + publish share one transaction (§5.3).
    /// </summary>
    public void MarkSupersedes(Guid supersededFareId)
    {
        SupersedesId = supersededFareId;
    }

    public static Result<Fare> Create(
        Guid? routeId,
        RouteCategory? category,
        Guid? fromZoneId,
        Guid? toZoneId,
        Money amount,
        DateOnly effectiveFrom,
        Guid proposedBy)
    {
        // Scope must be a route XOR a complete from/to zone pair (§4.2).
        var hasRoute = routeId is { } r && r != Guid.Empty;
        var hasZonePair = (fromZoneId is { } fz && fz != Guid.Empty)
            && (toZoneId is { } tz && tz != Guid.Empty);
        if (hasRoute == hasZonePair)
        {
            return Result.Failure<Fare>(FareErrors.ScopeInvalid);
        }

        // A route fare inherits its category from the route; a zone-pair fare
        // must name the category it prices (§6.2).
        if (hasRoute && category is not null)
        {
            return Result.Failure<Fare>(FareErrors.ScopeInvalid);
        }

        if (hasZonePair && category is null)
        {
            return Result.Failure<Fare>(FareErrors.ScopeInvalid);
        }

        if (hasZonePair && fromZoneId == toZoneId)
        {
            return Result.Failure<Fare>(FareErrors.ZonePairInvalid);
        }

        if (amount == Money.Zero || amount.AmountInKobo <= 0)
        {
            return Result.Failure<Fare>(FareErrors.AmountInvalid);
        }

        if (proposedBy == Guid.Empty)
        {
            return Result.Failure<Fare>(FareErrors.ScopeInvalid);
        }

        return new Fare(
            Guid.NewGuid(),
            hasRoute ? routeId : null,
            category,
            hasZonePair ? fromZoneId : null,
            hasZonePair ? toZoneId : null,
            amount,
            effectiveFrom,
            null,
            proposedBy,
            FareStatus.Draft);
    }

    /// <summary>Only drafts may be edited (§5.3: revise before submit).</summary>
    public Result UpdateAmount(Money amount)
    {
        if (Status != FareStatus.Draft)
        {
            return Result.Failure(FareErrors.NotEditable);
        }

        if (amount == Money.Zero || amount.AmountInKobo <= 0)
        {
            return Result.Failure(FareErrors.AmountInvalid);
        }

        Amount = amount;
        return Result.Success();
    }

    public Result UpdateEffectiveDates(DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        if (Status != FareStatus.Draft)
        {
            return Result.Failure(FareErrors.NotEditable);
        }

        if (effectiveTo is { } to && to < effectiveFrom)
        {
            return Result.Failure(FareErrors.EffectiveToBeforeFrom);
        }

        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        return Result.Success();
    }

    public Result SubmitForApproval()
    {
        if (Status != FareStatus.Draft)
        {
            return Result.Failure(FareErrors.NotSubmittable);
        }

        Status = FareStatus.PendingApproval;
        return Result.Success();
    }

    /// <summary>
    /// Approves the fare. The guard that the approver differs from the
    /// proposer is §5.3; the caller passes the current session user id.
    /// </summary>
    public Result Approve(Guid approverId, DateTime approvedAtUtc)
    {
        if (Status != FareStatus.PendingApproval)
        {
            return Result.Failure(FareErrors.NotApprovable);
        }

        if (approverId == ProposedBy)
        {
            return Result.Failure(FareErrors.SelfApproval);
        }

        if (approverId == Guid.Empty)
        {
            return Result.Failure(FareErrors.NotApprovable);
        }

        ApprovedBy = approverId;
        ApprovedAtUtc = approvedAtUtc;
        Status = FareStatus.Published;
        return Result.Success();
    }

    public Result Reject()
    {
        if (Status != FareStatus.PendingApproval)
        {
            return Result.Failure(FareErrors.NotApprovable);
        }

        Status = FareStatus.Rejected;
        return Result.Success();
    }

    /// <summary>
    /// Marks a published fare as replaced. The replacement fare is created by
    /// the application layer so supersede + publish share one transaction (§5.3).
    /// </summary>
    public Result Supersede(DateOnly effectiveTo)
    {
        if (Status != FareStatus.Published)
        {
            return Result.Failure(FareErrors.NotSupersedable);
        }

        if (effectiveTo < EffectiveFrom)
        {
            return Result.Failure(FareErrors.EffectiveToBeforeFrom);
        }

        EffectiveTo = effectiveTo;
        Status = FareStatus.Superseded;
        return Result.Success();
    }
}
