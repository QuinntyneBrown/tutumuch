using MediatR;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Checkouts;

namespace TuTuMuch.Worker;

/// <summary>Checkout settings of the worker.</summary>
public sealed class CheckoutJobOptions
{
    /// <summary>How often lapsed reservations are released; well inside the 10-minute window.</summary>
    public TimeSpan ExpiryJobInterval { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Releases checkouts whose 10 minutes have passed and puts their listings back on sale
/// (L2-028 AC3). Starting checkout also ignores a lapsed reservation, so the catalog is the only
/// thing that waits for this job.
/// </summary>
public sealed class ReservationExpiryJob(IServiceScopeFactory scopes, IOptions<CheckoutJobOptions> options, ILogger<ReservationExpiryJob> logger) : BackgroundService
{
    /// <summary>One pass: returns how many checkouts ended.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new ExpireReservationsCommand(), cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.ExpiryJobInterval);
        do
        {
            try
            {
                var expired = await RunOnceAsync(stoppingToken);
                if (expired > 0)
                {
                    logger.LogInformation("Released {Count} lapsed reservations", expired);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Releasing lapsed reservations failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
