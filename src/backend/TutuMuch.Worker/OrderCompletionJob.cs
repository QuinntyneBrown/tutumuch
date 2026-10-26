using MediatR;
using Microsoft.Extensions.Options;
using TutuMuch.Application.Orders;

namespace TutuMuch.Worker;

/// <summary>
/// Completes every Delivered order 3 days after delivery, which makes its payout releasable
/// (L2-034 AC2).
/// </summary>
public sealed class OrderCompletionJob(IServiceScopeFactory scopes, IOptions<OrderJobOptions> options, ILogger<OrderCompletionJob> logger) : BackgroundService
{
    /// <summary>One pass: returns how many orders were completed.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CompleteDeliveredOrdersCommand(), cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.CompletionJobInterval);
        do
        {
            try
            {
                var completed = await RunOnceAsync(stoppingToken);
                if (completed > 0)
                {
                    logger.LogInformation("Completed {Count} orders 3 days after delivery", completed);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Completing delivered orders failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
