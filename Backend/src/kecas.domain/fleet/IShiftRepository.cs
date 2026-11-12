namespace kecas.domain.fleet;

/// <summary>
/// Read-side repository for shifts. Public availability is DERIVED from open
/// shifts plus vehicle status — never typed by staff (DD-06).
/// </summary>
public interface IShiftRepository
{
    Task<Shift?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Shift>> ListAsync(
        int page,
        int pageSize,
        bool? openOnly = null,
        Guid? driverId = null,
        Guid? vehicleId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        bool? openOnly = null,
        Guid? driverId = null,
        Guid? vehicleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>All currently open shifts (ended_at IS NULL).</summary>
    Task<IReadOnlyList<Shift>> ListOpenAsync(CancellationToken cancellationToken = default);

    /// <summary>The open shift occupying a vehicle, if any (one per vehicle by partial unique index).</summary>
    Task<Shift?> FindOpenByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>The open shift for a driver, if any (one per driver by partial unique index).</summary>
    Task<Shift?> FindOpenByDriverAsync(Guid driverId, CancellationToken cancellationToken = default);

    /// <summary>Open shifts that started before the cutoff — the stale-shift guard (§5.1).</summary>
    Task<IReadOnlyList<Shift>> ListStaleOpenAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

    void Add(Shift shift);
}
