namespace kecas.application.fleet.ListDrivers;

/// <summary>Driver list (FR-06, FR-12 pagination).</summary>
public sealed record ListDriversQuery(int Page = 1, int PageSize = 20, Guid? OperatorId = null)
    : IQuery<ListDriversResponse>;
