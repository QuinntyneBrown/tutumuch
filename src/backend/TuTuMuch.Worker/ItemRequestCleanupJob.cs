using MediatR;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Requests;

namespace TuTuMuch.Worker;

/// <summary>How often the item request clean-up job runs; the "ItemRequests:Cleanup" configuration section.</summary>
public sealed class ItemRequestCleanupJobOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromDays(1);
}

/// <summary>Deletes item requests that expired at least 30 days ago, with their matches (L2-098 AC8).</summary>
public sealed class ItemRequestCleanupJob(IServiceScopeFactory scopes, IOptions<ItemRequestCleanupJobOptions> options, ILogger<ItemRequestCleanupJob> logger) : BackgroundService
{
    /// <summary>One pass: returns how many requests were deleted.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CleanUpItemRequestsCommand(), cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval);
        do
        {
            try
            {
                var deleted = await RunOnceAsync(stoppingToken);
                if (deleted > 0)
                {
                    logger.LogInformation("Deleted {Count} expired item requests", deleted);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Cleaning up expired item requests failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
