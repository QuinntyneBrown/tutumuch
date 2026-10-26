using MediatR;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Listings;

namespace TuTuMuch.Worker;

/// <summary>How often the draft clean-up job runs; the "Listings:DraftCleanup" configuration section.</summary>
public sealed class DraftCleanupJobOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromDays(1);
}

/// <summary>
/// Reminds sellers about drafts not edited for 83 days and deletes drafts not edited for 90 days,
/// with their photos (L2-019 AC4).
/// </summary>
public sealed class DraftCleanupJob(IServiceScopeFactory scopes, IOptions<DraftCleanupJobOptions> options, ILogger<DraftCleanupJob> logger) : BackgroundService
{
    /// <summary>One pass: returns how many drafts were reminded or deleted.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CleanUpDraftsCommand(), cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval);
        do
        {
            try
            {
                var handled = await RunOnceAsync(stoppingToken);
                if (handled > 0)
                {
                    logger.LogInformation("Reminded about or deleted {Count} stale drafts", handled);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Cleaning up stale drafts failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
