namespace kecas.application.network;

/// <summary>Shared response shapes for the network module (FR-07).</summary>
public sealed record ZoneListItem(Guid Id, string Code, string Name);

public sealed record ListZonesResponse(IReadOnlyList<ZoneListItem> Items, int Total);

public sealed record StopListItem(
    Guid Id,
    string Code,
    string Name,
    Guid ZoneId,
    string ZoneCode,
    bool IsRank);

public sealed record ListStopsResponse(IReadOnlyList<StopListItem> Items, int Total);

public sealed record RouteStopItem(string StopCode, string StopName, int Seq, int MinsFromStart);

public sealed record RouteListItem(
    Guid Id,
    string Code,
    string Name,
    string Category,
    string? Description,
    bool IsActive,
    IReadOnlyList<RouteStopItem> Stops);

public sealed record ListRoutesResponse(IReadOnlyList<RouteListItem> Items, int Page, int PageSize, int Total);
