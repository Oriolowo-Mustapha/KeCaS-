namespace kecas.infrastructure.Repositories;

using kecas.application.abstractions.data;
using kecas.domain.users;
using Microsoft.EntityFrameworkCore;

internal sealed class ExistenceChecker(ApplicationDbContext context) : IExistenceChecker
{
    public Task<bool> UserEmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        context.Users.AnyAsync(u => u.Email == new Email(normalizedEmail), cancellationToken);

    public Task<bool> VehiclePlateNumberExistsAsync(string plateNumber, CancellationToken cancellationToken = default) =>
        context.Vehicles.AnyAsync(v => v.PlateNumber == new PlateNumber(plateNumber), cancellationToken);

    public Task<bool> VehicleCodeExistsAsync(string vehicleCode, CancellationToken cancellationToken = default) =>
        context.Vehicles.AnyAsync(v => v.Code == new VehicleCode(vehicleCode), cancellationToken);
}
