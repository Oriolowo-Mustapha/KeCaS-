namespace kecas.infrastructure.Repositories;

using kecas.domain.fleet;
using kecas.domain.users;
using Microsoft.EntityFrameworkCore;

internal sealed class VehicleRepository(ApplicationDbContext context) : IVehicleRepository
{
    public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<Vehicle?> GetByPlateAsync(string plateNumber, CancellationToken cancellationToken = default) =>
        context.Vehicles.FirstOrDefaultAsync(v => v.PlateNumber == new PlateNumber(plateNumber), cancellationToken);

    public async Task<IReadOnlyList<Vehicle>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var vehicles = await context.Vehicles
            .AsNoTracking()
            .Where(v => ids.Contains(v.Id))
            .ToListAsync(cancellationToken);

        return vehicles;
    }

    public async Task<IReadOnlyList<Vehicle>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var vehicles = await context.Vehicles
            .AsNoTracking()
            .OrderBy(v => v.Code)
            .ToListAsync(cancellationToken);

        return vehicles;
    }

    public async Task<IReadOnlyList<Vehicle>> ListAsync(
        int page,
        int pageSize,
        VehicleCategory? category = null,
        VehicleStatus? status = null,
        Guid? operatorId = null,
        CancellationToken cancellationToken = default)
    {
        var query = Filter(category, status, operatorId);

        return await query
            .AsNoTracking()
            .OrderBy(v => v.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(
        VehicleCategory? category = null,
        VehicleStatus? status = null,
        Guid? operatorId = null,
        CancellationToken cancellationToken = default) =>
        Filter(category, status, operatorId).CountAsync(cancellationToken);

    private IQueryable<Vehicle> Filter(
        VehicleCategory? category,
        VehicleStatus? status,
        Guid? operatorId)
    {
        var query = context.Vehicles.AsQueryable();

        if (category.HasValue)
        {
            query = query.Where(v => v.Category == category.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(v => v.Status == status.Value);
        }

        if (operatorId.HasValue)
        {
            query = query.Where(v => v.OperatorId == operatorId.Value);
        }

        return query;
    }

    public Task<bool> PlateNumberExistsAsync(string plateNumber, CancellationToken cancellationToken = default) =>
        context.Vehicles.AnyAsync(v => v.PlateNumber == new PlateNumber(plateNumber), cancellationToken);

    public Task<bool> CodeExistsAsync(string vehicleCode, CancellationToken cancellationToken = default) =>
        context.Vehicles.AnyAsync(v => v.Code == new VehicleCode(vehicleCode), cancellationToken);

    public void Add(Vehicle vehicle) => context.Vehicles.Add(vehicle);
}

internal sealed class OperatorRepository(ApplicationDbContext context) : IOperatorRepository
{
    public Task<Operator?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Operators.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Operators.AnyAsync(o => o.Id == id, cancellationToken);

    public void Add(Operator @operator) => context.Operators.Add(@operator);
}
