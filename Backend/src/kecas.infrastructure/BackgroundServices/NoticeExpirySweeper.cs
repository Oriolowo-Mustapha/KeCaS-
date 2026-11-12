namespace kecas.infrastructure.BackgroundServices;

using System.Text.Json;
using kecas.application.abstractions.clock;
using kecas.application.abstractions.data;
using kecas.domain.notices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// The notice expiry job (FR-22, §3.1 scheduled jobs): moves PUBLISHED notices
/// whose end time has passed to EXPIRED so they leave the public board, with an
/// audit row (system actor). Runs hourly by default
/// (NoticeExpiry:IntervalMinutes to override).
/// </summary>
internal sealed class NoticeExpirySweeper(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IDateTimeProvider dateTimeProvider,
    ILogger<NoticeExpirySweeper> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(
            configuration.GetValue("NoticeExpiry:IntervalMinutes", 60));

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
                // will pick the notices up.
                logger.LogError(ex, "The notice expiry sweep failed and will retry next cycle.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        var nowUtc = dateTimeProvider.UtcNow;

        await using var scope = scopeFactory.CreateAsyncScope();
        var noticeRepository = scope.ServiceProvider.GetRequiredService<INoticeRepository>();
        var auditWriter = scope.ServiceProvider.GetRequiredService<IAuditWriter>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var expired = await noticeRepository.ListExpiredPublishedAsync(nowUtc, cancellationToken);
        if (expired.Count == 0)
        {
            return;
        }

        foreach (var notice in expired)
        {
            var result = notice.Expire();
            if (result.IsFailure)
            {
                logger.LogWarning(
                    "Could not expire notice {NoticeId}: {Error}",
                    notice.Id, result.Error.Code);
                continue;
            }

            auditWriter.Append(
                actorId: null,
                "notice.expired_auto",
                "notice",
                notice.Id.ToString(),
                before: JsonSerializer.Serialize(new { status = "PUBLISHED" }),
                after: JsonSerializer.Serialize(new { status = notice.Status.ToStorageValue() }));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Auto-expired {Count} notice(s).", expired.Count);
    }
}
