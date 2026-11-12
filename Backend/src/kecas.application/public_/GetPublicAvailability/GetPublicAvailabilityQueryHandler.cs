namespace kecas.application.public_.GetPublicAvailability;

using kecas.application.abstractions.clock;
using kecas.domain.fleet;
using kecas.domain.network;
using kecas.domain.trips;

/// <summary>
/// GET /public/availability?category=&amp;stop= (FR-11, §5.1/§6.2): evaluates
/// the availability rules per vehicle, first match wins, and returns COUNTS BY
/// VALUE — available, assigned, unavailable, under maintenance. No individual
/// driver data (NFR-05). ?stop= counts vehicles checked in at that rank.
/// </summary>
internal sealed class GetPublicAvailabilityQueryHandler(
    IVehicleRepository vehicleRepository,
    IShiftRepository shiftRepository,
    ITripRepository tripRepository,
    IStopRepository stopRepository,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetPublicAvailabilityQuery, PublicAvailabilityResponse>
{
    public async Task<Result<PublicAvailabilityResponse>> Handle(
        GetPublicAvailabilityQuery query,
        CancellationToken cancellationToken)
    {
        var nowUtc = dateTimeProvider.UtcNow;

        var vehicles = await vehicleRepository.ListAllAsync(cancellationToken);

        // Rule 3 horizon: scheduled trips starting within 30 minutes (§5.1).
        var liveTrips = await tripRepository.ListLiveForAvailabilityAsync(
            nowUtc, nowUtc.AddMinutes(30), cancellationToken);

        var openShifts = await shiftRepository.ListOpenAsync(cancellationToken);
        var openShiftByVehicle = openShifts
            .GroupBy(s => s.VehicleId)
            .ToDictionary(g => g.Key, g => g.First());

        // ?stop= — only vehicles checked in at that rank stop can be counted.
        Guid? stopFilterId = null;
        if (!string.IsNullOrWhiteSpace(query.StopCode))
        {
            var stopCode = query.StopCode.Trim().ToUpperInvariant();
            stopFilterId = (await stopRepository.ListAsync(cancellationToken: cancellationToken))
                .FirstOrDefault(s => s.Code == stopCode)?.Id;

            if (stopFilterId is null)
            {
                // Unknown stop: no vehicle can be waiting there.
                return Result.Success(new PublicAvailabilityResponse(nowUtc, [], 0));
            }
        }

        var categoryFilter = string.IsNullOrWhiteSpace(query.Category)
            ? null
            : query.Category.Trim().ToUpperInvariant();

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var total = 0;

        foreach (var vehicle in vehicles)
        {
            if (categoryFilter is not null
                && vehicle.Category.ToString().ToUpperInvariant() != categoryFilter)
            {
                continue;
            }

            // §5.1 rules, first match wins.
            var value =
                vehicle.Status == VehicleStatus.Maintenance ? "UNDER_MAINTENANCE"
                : vehicle.Status is VehicleStatus.Inactive or VehicleStatus.Retired ? "UNAVAILABLE"
                : liveTrips.Any(t => t.VehicleId == vehicle.Id) ? "ASSIGNED"
                : openShiftByVehicle.ContainsKey(vehicle.Id) ? "AVAILABLE"
                : "UNAVAILABLE";

            // A stop filter narrows the view to vehicles waiting at that rank.
            if (stopFilterId is not null
                && (!openShiftByVehicle.TryGetValue(vehicle.Id, out var shift)
                    || shift.RankStopId != stopFilterId))
            {
                continue;
            }

            counts[value] = counts.GetValueOrDefault(value) + 1;
            total++;
        }

        var order = new[] { "AVAILABLE", "ASSIGNED", "UNDER_MAINTENANCE", "UNAVAILABLE" };
        var items = order
            .Where(v => counts.ContainsKey(v))
            .Select(v => new PublicAvailabilityCountItem(v, counts[v]))
            .ToList();

        return Result.Success(new PublicAvailabilityResponse(nowUtc, items, total));
    }
}
