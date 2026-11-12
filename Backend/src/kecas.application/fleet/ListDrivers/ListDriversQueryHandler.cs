namespace kecas.application.fleet.ListDrivers;

using kecas.application.abstractions.data;

internal sealed class ListDriversQueryHandler(IDriverRepository driverRepository)
    : IQueryHandler<ListDriversQuery, ListDriversResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<ListDriversResponse>> Handle(
        ListDriversQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var drivers = await driverRepository.ListAsync(page, pageSize, query.OperatorId, cancellationToken);
        var total = await driverRepository.CountAsync(query.OperatorId, cancellationToken);

        var items = drivers.Select(d => new DriverListItem(
            d.Id,
            d.UserId,
            d.OperatorId,
            LicenceMasker.Mask(d.LicenceNumber),
            d.LicenceExpiry,
            d.Status.ToString())).ToList();

        return Result.Success(new ListDriversResponse(items, page, pageSize, total));
    }
}
