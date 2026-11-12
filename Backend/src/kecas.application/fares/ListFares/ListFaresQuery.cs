namespace kecas.application.fares.ListFares;

using kecas.domain.fares;

public sealed record ListFaresQuery(
    int Page,
    int PageSize,
    FareStatus? Status,
    Guid? RouteId) : IQuery<ListFaresResponse>;
