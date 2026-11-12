namespace kecas.application.abstractions.data;

/// <summary>
/// Read-side abstraction for queries that need row existence checks without
/// loading entities. Implemented in Infrastructure.
/// </summary>
public interface IExistenceChecker
{
    Task<bool> UserEmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<bool> VehiclePlateNumberExistsAsync(string plateNumber, CancellationToken cancellationToken = default);

    Task<bool> VehicleCodeExistsAsync(string vehicleCode, CancellationToken cancellationToken = default);
}
