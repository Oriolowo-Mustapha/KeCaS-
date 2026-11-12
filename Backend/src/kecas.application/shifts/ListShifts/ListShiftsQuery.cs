namespace kecas.application.shifts.ListShifts;

public sealed record ListShiftsQuery(
    int Page,
    int PageSize,
    bool? OpenOnly,
    Guid? DriverId,
    Guid? VehicleId) : IQuery<ListShiftsResponse>;
