namespace kecas.application.fleet.ListVehicles;

using kecas.domain.common;

/// <summary>Vehicle list with filters (FR-12 conventions: page/page_size, category, status).</summary>
public sealed record ListVehiclesQuery(
    int Page = 1,
    int PageSize = 20,
    VehicleCategory? Category = null,
    VehicleStatus? Status = null,
    Guid? OperatorId = null) : IQuery<ListVehiclesResponse>;
