namespace kecas.application.fares;

using kecas.domain.network;

/// <summary>Shared response shapes for the fares module (FR-08/09).</summary>
public sealed record FareListItem(
    Guid Id,
    string Scope,
    Guid? RouteId,
    string? RouteCode,
    string? Category,
    Guid? FromZoneId,
    string? FromZoneCode,
    Guid? ToZoneId,
    string? ToZoneCode,
    int AmountKobo,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string Status,
    Guid ProposedBy,
    Guid? ApprovedBy,
    DateTime? ApprovedAtUtc,
    Guid? SupersedesId);

public sealed record ListFaresResponse(IReadOnlyList<FareListItem> Items, int Page, int PageSize, int Total);

/// <summary>
/// Maps a Fare aggregate to its API shape, resolving route codes/categories
/// and zone codes through the loaded dictionaries. Scope strings: "route" or
/// "zones" (Â§4.2).
/// </summary>
public static class FareMapper
{
    public static FareListItem ToListItem(
        kecas.domain.fares.Fare fare,
        IReadOnlyDictionary<Guid, Route> routeById,
        IReadOnlyDictionary<Guid, string> zoneCodeById)
    {
        var isRouteScope = fare.RouteId is not null;
        var route = fare.RouteId is { } routeId ? routeById.GetValueOrDefault(routeId) : null;

        return new FareListItem(
            fare.Id,
            isRouteScope ? "route" : "zones",
            fare.RouteId,
            route?.Code,
            // Route fares follow the route's category; zone-pair fares carry their own.
            fare.Category is { } category
                ? category.ToString().ToUpperInvariant()
                : route is null ? null : route.Category.ToString().ToUpperInvariant(),
            fare.FromZoneId,
            fare.FromZoneId is { } fromId && zoneCodeById.TryGetValue(fromId, out var fromCode)
                ? fromCode
                : null,
            fare.ToZoneId,
            fare.ToZoneId is { } toId && zoneCodeById.TryGetValue(toId, out var toCode)
                ? toCode
                : null,
            fare.Amount.AmountInKobo,
            fare.EffectiveFrom,
            fare.EffectiveTo,
            fare.Status.ToStorageValue(),
            fare.ProposedBy,
            fare.ApprovedBy,
            fare.ApprovedAtUtc,
            fare.SupersedesId);
    }
}
