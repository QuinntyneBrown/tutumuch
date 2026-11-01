using MediatR;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Grabs;

namespace TuTuMuch.Worker;

/// <summary>How often the grab deadline job runs; the "Grabs:Deadlines" configuration section.</summary>
public sealed class GrabDeadlineJobOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Expires grabs TuTuMuch did not accept within 48 hours or buy within 3 days, with full refunds,
/// and marks Bought grabs Received after 14 days (L2-119, ADR orders/0009).
/// </summary>
public sealed class GrabDeadlineJob(IServiceScopeFactory scopes, IOptions<GrabDeadlineJobOptions> options, ILogger<GrabDeadlineJob> logger) : BackgroundService
{
    /// <summary>One pass, in its own scope.</summary>
    public async Task<GrabDeadlineResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RunGrabDeadlinesCommand(), cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval);
        do
        {
            try
            {
                var result = await RunOnceAsync(stoppingToken);
                if (result.Expired + result.Received > 0)
                {
                    logger.LogInformation("Expired {Expired} grabs and received {Received}", result.Expired, result.Received);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Running grab deadlines failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
