using MediatR;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Listings;

namespace TuTuMuch.Worker;

/// <summary>Auction settings of the worker.</summary>
public sealed class AuctionJobOptions
{
    /// <summary>How often ended auctions and payment windows are closed; at least once a minute (L2-117).</summary>
    public TimeSpan CloseJobInterval { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Closes auctions whose end time has passed and ends payment windows that passed unpaid (L2-117,
/// L2-118). Bids are refused after the end time anyway, so the outcome never waits for this job.
/// </summary>
public sealed class AuctionCloseJob(IServiceScopeFactory scopes, IOptions<AuctionJobOptions> options, ILogger<AuctionCloseJob> logger) : BackgroundService
{
    /// <summary>One pass: returns how many auctions changed.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CloseAuctionsCommand(), cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.CloseJobInterval);
        do
        {
            try
            {
                var closed = await RunOnceAsync(stoppingToken);
                if (closed > 0)
                {
                    logger.LogInformation("Closed {Count} auctions or payment windows", closed);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Closing auctions failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
