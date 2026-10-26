namespace TutuMuch.Worker;

/// <summary>Host for background jobs; each job is its own hosted service, such as <see cref="ReservationExpiryJob"/> and <see cref="ShipDeadlineJob"/>.</summary>
public sealed class Worker : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
