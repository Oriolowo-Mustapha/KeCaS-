namespace kecas.application.public_.GetPublicFares;

using kecas.domain.network;

internal sealed class GetPublicFaresQueryHandler(
    IFareRepository fareRepository,
    IRouteRepository routeRepository,
    IZoneRepository zoneRepository)
    : IQueryHandler<GetPublicFaresQuery, PublicFaresResponse>
{
    public async Task<Result<PublicFaresResponse>> Handle(
        GetPublicFaresQuery query,
        CancellationToken cancellationToken)
    {
        var fares = await fareRepository.ListPublishedAsync(cancellationToken);

        // Resolve codes so the public API never leaks internal ids.
        var zones = await zoneRepository.ListAsync(cancellationToken);
        var zoneCodeById = zones.ToDictionary(z => z.Id, z => z.Code);

        var routes = await routeRepository.ListAsync(cancellationToken: cancellationToken);
        var routeById = routes.ToDictionary(r => r.Id);

        var categoryFilter = NormalizeCategory(query.Category);
        var fromFilter = NormalizeZoneCode(query.FromZoneCode);
        var toFilter = NormalizeZoneCode(query.ToZoneCode);

        var items = fares
            .Select(f =>
            {
                var route = f.RouteId is { } routeId ? routeById.GetValueOrDefault(routeId) : null;
                return new
                {
                    Fare = f,
                    RouteCode = route?.Code,
                    // Route fares inherit the route's category; zone-pair fares carry their own.
                    Category = f.Category is { } category
                        ? category.ToString().ToUpperInvariant()
                        : route is null ? null : route.Category.ToString().ToUpperInvariant(),
                    FromZoneCode = f.FromZoneId is { } fromId && zoneCodeById.TryGetValue(fromId, out var fromCode)
                        ? fromCode
                        : null,
                    ToZoneCode = f.ToZoneId is { } toId && zoneCodeById.TryGetValue(toId, out var toCode)
                        ? toCode
                        : null,
                };
            })
            .Where(x => MatchesFilter(x.Category, x.FromZoneCode, x.ToZoneCode, categoryFilter, fromFilter, toFilter))
            .Select(x => new PublicFareItem(
                x.Fare.RouteId is not null ? "route" : "zones",
                x.RouteCode,
                x.Category,
                x.FromZoneCode,
                x.ToZoneCode,
                x.Fare.Amount.AmountInKobo,
                x.Fare.EffectiveFrom,
                x.Fare.EffectiveTo,
                x.Fare.ApprovedAtUtc))
            .OrderBy(f => f.RouteCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f.FromZoneCode, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result.Success(new PublicFaresResponse(items));
    }

    private static string? NormalizeZoneCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();

    private static string? NormalizeCategory(string? category) =>
        string.IsNullOrWhiteSpace(category) ? null : category.Trim().ToUpperInvariant();

    private static bool MatchesFilter(
        string? category,
        string? fromZoneCode,
        string? toZoneCode,
        string? categoryFilter,
        string? fromFilter,
        string? toFilter)
    {
        if (categoryFilter is not null && !string.Equals(category, categoryFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // No zone filter: everything shows. With a zone filter: only zone-pair
        // fares matching the requested direction show (route fares are
        // excluded because a from/to query asks about zone-to-zone travel).
        if (fromFilter is null && toFilter is null)
        {
            return true;
        }

        return fromZoneCode is not null
            && toZoneCode is not null
            && (fromFilter is null || fromZoneCode == fromFilter)
            && (toFilter is null || toZoneCode == toFilter);
    }
}
