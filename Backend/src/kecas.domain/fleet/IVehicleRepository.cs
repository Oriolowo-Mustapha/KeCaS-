namespace kecas.domain.fleet;

/// <summary>
/// Repository for the Vehicle aggregate root (FR-04, FR-05). Implemented by
/// Infrastructure on top of EF Core.
/// </summary>
public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehicle>> ListAsync(
        int page,
        int pageSize,
        VehicleCategory? category = null,
        VehicleStatus? status = null,
        Guid? operatorId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        VehicleCategory? category = null,
        VehicleStatus? status = null,
        Guid? operatorId = null,
        CancellationToken cancellationToken = default);

    Task<Vehicle?> GetByPlateAsync(string plateNumber, CancellationToken cancellationToken = default);

    /// <summary>Bulk lookup used by public availability projections.</summary>
    Task<IReadOnlyList<Vehicle>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>The whole fleet, for the availability view (§5.1).</summary>
    Task<IReadOnlyList<Vehicle>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<bool> PlateNumberExistsAsync(string plateNumber, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string vehicleCode, CancellationToken cancellationToken = default);

    void Add(Vehicle vehicle);
}
