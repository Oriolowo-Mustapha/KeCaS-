namespace kecas.infrastructure.BackgroundServices;

using System.Text.Json;
using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;
using kecas.domain.fleet;
using kecas.infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// The stale-shift guard (design doc §5.1): a driver who forgets to check out
/// would leave a vehicle showing as available all night. Periodically closes
/// any shift open for more than the guard window, marking it ClosedBy = AUTO
/// with an audit row. Runs hourly; the window is 14 hours by default
/// (StaleShift__MaxOpenHours / StaleShift__IntervalMinutes to override).
/// </summary>
internal sealed class StaleShiftSweeper(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IDateTimeProvider dateTimeProvider,
    ILogger<StaleShiftSweeper> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(
            configuration.GetValue("StaleShift:IntervalMinutes", 60));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down — nothing to do.
            }
            catch (Exception ex)
            {
                // Never let one failed sweep kill the service; the next run
                // will pick the shifts up.
                logger.LogError(ex, "The stale-shift sweep failed and will retry next cycle.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        var maxOpenHours = configuration.GetValue("StaleShift:MaxOpenHours", 14);
        var cutoffUtc = dateTimeProvider.UtcNow.AddHours(-maxOpenHours);

        await using var scope = scopeFactory.CreateAsyncScope();
        var shiftRepository = scope.ServiceProvider.GetRequiredService<IShiftRepository>();
        var auditWriter = scope.ServiceProvider.GetRequiredService<IAuditWriter>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var staleShifts = await shiftRepository.ListStaleOpenAsync(cutoffUtc, cancellationToken);
        if (staleShifts.Count == 0)
        {
            return;
        }

        foreach (var shift in staleShifts)
        {
            var result = shift.CheckOut(dateTimeProvider.UtcNow, ShiftClosedBy.Auto);
            if (result.IsFailure)
            {
                logger.LogWarning(
                    "Could not auto-close shift {ShiftId}: {Error}",
                    shift.Id, result.Error.Code);
                continue;
            }

            auditWriter.Append(
                actorId: null,
                "shift.closed_auto",
                "shift",
                shift.Id.ToString(),
                before: JsonSerializer.Serialize(new { endedAt = (DateTime?)null }),
                after: JsonSerializer.Serialize(new
                {
                    endedAt = shift.EndedAt,
                    reason = $"Shift open longer than {maxOpenHours}h (stale-shift guard).",
                }));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Auto-closed {Count} stale shift(s).", staleShifts.Count);
    }
}
