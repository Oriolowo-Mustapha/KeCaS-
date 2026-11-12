namespace kecas.infrastructure.Repositories;

using kecas.application.abstractions.data;
using kecas.infrastructure.Configurations;
using kecas.domain.fares;
using kecas.domain.fleet;
using kecas.domain.network;
using kecas.domain.trips;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// FR-19: the five default reports (Section 14: trips by period, fleet status,
/// driver activity, availability history, fare history). Aggregations run over
/// the existing tables — no extra report tables or views needed for the
/// prototype — and periods filter on the trip/shift/fare's own date columns.
/// </summary>
internal sealed class ReportQueries(ApplicationDbContext context) : IReportQueries
{
    public async Task<IReadOnlyList<TripsDayRow>> TripsByDayAsync(
        DateTime? from, DateTime? to, Guid? routeId, Guid? vehicleId,
        CancellationToken cancellationToken = default)
    {
        var query = context.Trips.AsNoTracking().AsQueryable();
        if (from.HasValue)
        {
            query = query.Where(t => t.StartsAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(t => t.StartsAt <= to.Value);
        }

        if (routeId.HasValue)
        {
            query = query.Where(t => t.RouteId == routeId.Value);
        }

        if (vehicleId.HasValue)
        {
            query = query.Where(t => t.VehicleId == vehicleId.Value);
        }

        // Prototype-scale data: pull (date, status) pairs and aggregate in
        // memory — avoids relying on DATE_TRUNC grouping translations.
        var tuples = await query
            .Select(t => new { t.StartsAt, t.Status })
            .ToListAsync(cancellationToken);

        return tuples
            .GroupBy(t => DateOnly.FromDateTime(t.StartsAt))
            .OrderBy(g => g.Key)
            .Select(g => new TripsDayRow(
                g.Key,
                g.Count(x => x.Status == TripStatus.Scheduled),
                g.Count(x => x.Status == TripStatus.InProgress),
                g.Count(x => x.Status == TripStatus.Completed),
                g.Count(x => x.Status == TripStatus.Cancelled),
                g.Count()))
            .ToList();
    }

    public async Task<IReadOnlyList<FleetStatusRow>> FleetByStatusAsync(
        VehicleCategory? category, Guid? operatorId,
        CancellationToken cancellationToken = default)
    {
        var query = context.Vehicles.AsNoTracking().AsQueryable();
        if (category.HasValue)
        {
            query = query.Where(v => v.Category == category.Value);
        }

        if (operatorId.HasValue)
        {
            query = query.Where(v => v.OperatorId == operatorId.Value);
        }

        var tuples = await query
            .Select(v => new { v.Category, v.Status })
            .ToListAsync(cancellationToken);

        return tuples
            .GroupBy(v => new { v.Category, v.Status })
            .OrderBy(g => g.Key.Category)
            .ThenBy(g => g.Key.Status)
            .Select(g => new FleetStatusRow(
                EnumStorage.VehicleCategoryTo(g.Key.Category),
                EnumStorage.VehicleStatusTo(g.Key.Status),
                g.Count()))
            .ToList();
    }

    public async Task<IReadOnlyList<DriverActivityRow>> DriverActivityAsync(
        DateTime? from, DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = context.Trips.AsNoTracking()
            .Where(t => t.DriverId != null);
        if (from.HasValue)
        {
            query = query.Where(t => t.StartsAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(t => t.StartsAt <= to.Value);
        }

        var tuples = await query
            .Select(t => new { DriverId = t.DriverId!.Value, t.Status })
            .ToListAsync(cancellationToken);

        var countsByDriver = tuples
            .GroupBy(t => t.DriverId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Scheduled = g.Count(x => x.Status == TripStatus.Scheduled),
                    Completed = g.Count(x => x.Status == TripStatus.Completed),
                    Cancelled = g.Count(x => x.Status == TripStatus.Cancelled),
                    Total = g.Count(),
                });

        // Names come from the linked user account; drivers without one stay
        // unnamed (their id still identifies the row).
        var driverIds = countsByDriver.Keys.ToList();
        var userNames = await context.Drivers
            .AsNoTracking()
            .Where(d => driverIds.Contains(d.Id) && d.UserId != null)
            .Select(d => new { d.Id, d.UserId })
            .ToListAsync(cancellationToken);
        var userIds = userNames.Select(d => d.UserId!.Value).ToList();
        var names = await context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);
        var nameByDriver = userNames
            .Where(d => d.UserId.HasValue && names.ContainsKey(d.UserId.Value))
            .ToDictionary(d => d.Id, d => names[d.UserId!.Value]);

        return countsByDriver
            .OrderBy(k => nameByDriver.TryGetValue(k.Key, out var name) ? name : k.Key.ToString())
            .Select(k => new DriverActivityRow(
                k.Key,
                nameByDriver.TryGetValue(k.Key, out var name) ? name : null,
                k.Value.Scheduled,
                k.Value.Completed,
                k.Value.Cancelled,
                k.Value.Total))
            .ToList();
    }

    public async Task<IReadOnlyList<AvailabilityDayRow>> AvailabilityHistoryAsync(
        DateTime? from, DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var query = context.Shifts.AsNoTracking().AsQueryable();
        if (from.HasValue)
        {
            query = query.Where(s => s.EndedAt == null || s.EndedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.StartedAt <= to.Value);
        }

        var tuples = await query
            .Select(s => new { s.StartedAt, s.EndedAt })
            .ToListAsync(cancellationToken);

        var checkInsByDay = tuples
            .GroupBy(s => DateOnly.FromDateTime(s.StartedAt))
            .ToDictionary(g => g.Key, g => g.Count());
        var checkOutsByDay = tuples
            .Where(s => s.EndedAt.HasValue)
            .GroupBy(s => DateOnly.FromDateTime(s.EndedAt!.Value))
            .ToDictionary(g => g.Key, g => g.Count());

        // Open at end of day: started on/before the day, not closed by its end.
        var days = checkInsByDay.Keys.Union(checkOutsByDay.Keys).OrderBy(d => d);
        return days
            .Select(day =>
            {
                var dayEnd = day.ToDateTime(new TimeOnly(23, 59, 59));
                return new AvailabilityDayRow(
                    day,
                    checkInsByDay.TryGetValue(day, out var ins) ? ins : 0,
                    checkOutsByDay.TryGetValue(day, out var outs) ? outs : 0,
                    tuples.Count(s =>
                        DateOnly.FromDateTime(s.StartedAt) <= day &&
                        (!s.EndedAt.HasValue || s.EndedAt.Value > dayEnd)));
            })
            .ToList();
    }

    public async Task<IReadOnlyList<FareHistoryRow>> FareHistoryAsync(
        RouteCategory? category,
        CancellationToken cancellationToken = default)
    {
        var query = context.Fares.AsNoTracking().AsQueryable();
        if (category.HasValue)
        {
            query = query.Where(f => f.Category == category.Value);
        }

        var fares = await query
            .OrderByDescending(f => f.EffectiveFrom)
            .ToListAsync(cancellationToken);

        // Zone-pair fares have no route; route fares show their route code.
        var routeIds = fares.Where(f => f.RouteId.HasValue).Select(f => f.RouteId!.Value).ToList();
        var routeCodes = routeIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await context.Routes
                .AsNoTracking()
                .Where(r => routeIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Code, cancellationToken);

        return fares
            .Select(f => new FareHistoryRow(
                f.Id,
                f.RouteId.HasValue && routeCodes.TryGetValue(f.RouteId.Value, out var code) ? code : null,
                f.Category.HasValue ? EnumStorage.RouteCategoryTo(f.Category.Value) : null,
                f.Amount.AmountInKobo,
                f.Status.ToStorageValue(),
                f.EffectiveFrom,
                f.EffectiveTo,
                f.ApprovedAtUtc))
            .ToList();
    }
}
