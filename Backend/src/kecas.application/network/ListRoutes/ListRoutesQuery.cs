namespace kecas.application.network.ListRoutes;

using kecas.domain.network;

public sealed record ListRoutesQuery(
    RouteCategory? Category,
    bool? IsActive,
    int Page,
    int PageSize) : IQuery<ListRoutesResponse>;
