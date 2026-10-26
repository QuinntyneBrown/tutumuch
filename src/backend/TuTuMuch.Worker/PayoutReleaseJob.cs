using MediatR;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Payouts;

namespace TuTuMuch.Worker;

public sealed class PayoutJobOptions
{
    /// <summary>How often the release job looks for Releasable payouts.</summary>
    public TimeSpan ReleaseJobInterval { get; set; } = TimeSpan.FromMinutes(15);
}

/// <summary>
/// Sends every Releasable payout of a Completed order to an onboarded seller (L2-031 AC3). Orders
/// that are Paid, Shipped or Delivered never match.
/// </summary>
public sealed class PayoutReleaseJob(IServiceScopeFactory scopes, IOptions<PayoutJobOptions> options, ILogger<PayoutReleaseJob> logger) : BackgroundService
{
    /// <summary>One pass: returns how many payouts were sent.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new ReleasePayoutsCommand(), cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.ReleaseJobInterval);
        do
        {
            try
            {
                var sent = await RunOnceAsync(stoppingToken);
                if (sent > 0)
                {
                    logger.LogInformation("Sent {Count} payouts", sent);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Releasing payouts failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
