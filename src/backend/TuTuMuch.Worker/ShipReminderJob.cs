using MediatR;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Orders;

namespace TuTuMuch.Worker;

/// <summary>
/// Reminds the seller of every Paid order with no tracking 3 business days after payment, once,
/// by email (L2-033 AC3).
/// </summary>
public sealed class ShipReminderJob(IServiceScopeFactory scopes, IOptions<EmailDispatchJobOptions> options, ILogger<ShipReminderJob> logger) : BackgroundService
{
    /// <summary>One pass: returns how many sellers were reminded.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new SendShipRemindersCommand(), cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.ShipReminderJobInterval);
        do
        {
            try
            {
                var reminded = await RunOnceAsync(stoppingToken);
                if (reminded > 0)
                {
                    logger.LogInformation("Reminded {Count} sellers to ship", reminded);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Reminding sellers to ship failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
