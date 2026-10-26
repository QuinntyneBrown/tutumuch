using MediatR;
using Microsoft.Extensions.Options;
using TutuMuch.Application.Notifications;

namespace TutuMuch.Worker;

/// <summary>Email settings of the worker.</summary>
public sealed class EmailDispatchJobOptions
{
    /// <summary>How often due outbox messages are sent; short, so an email leaves within 2 minutes (L2-042).</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>How often Paid orders without tracking 3 business days after payment remind their seller (L2-033 AC3).</summary>
    public TimeSpan ShipReminderJobInterval { get; set; } = TimeSpan.FromMinutes(15);
}

/// <summary>
/// Sends the emails queued in the outbox (L2-042): each pass claims a batch of due messages and
/// sends them through the email provider, retrying failures with back-off.
/// </summary>
public sealed class EmailDispatchJob(IServiceScopeFactory scopes, IOptions<EmailDispatchJobOptions> options, ILogger<EmailDispatchJob> logger) : BackgroundService
{
    /// <summary>One pass: returns how many emails were sent.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new DispatchEmailsCommand(), cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollInterval);
        do
        {
            try
            {
                // Keep going while full batches come back, so a backlog drains between ticks.
                while (await RunOnceAsync(stoppingToken) >= DispatchEmailsCommand.BatchSize)
                {
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Dispatching emails failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
