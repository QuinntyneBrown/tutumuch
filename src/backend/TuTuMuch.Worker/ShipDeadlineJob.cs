using MediatR;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Orders;

namespace TuTuMuch.Worker;

/// <summary>Order settings of the worker.</summary>
public sealed class OrderJobOptions
{
    /// <summary>How often Paid orders past their ship-by date are cancelled and refunded.</summary>
    public TimeSpan DeadlineJobInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>How often Delivered orders 3 days after delivery are completed.</summary>
    public TimeSpan CompletionJobInterval { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Cancels every Paid order with no tracking by its ship-by date, 5 business days after payment,
/// and refunds the buyer in full (L2-033 AC2).
/// </summary>
public sealed class ShipDeadlineJob(IServiceScopeFactory scopes, IOptions<OrderJobOptions> options, ILogger<ShipDeadlineJob> logger) : BackgroundService
{
    /// <summary>One pass: returns how many orders were cancelled.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CancelUnshippedOrdersCommand(), cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.DeadlineJobInterval);
        do
        {
            try
            {
                var cancelled = await RunOnceAsync(stoppingToken);
                if (cancelled > 0)
                {
                    logger.LogInformation("Cancelled and refunded {Count} orders past their ship-by date", cancelled);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Cancelling orders past their ship-by date failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
