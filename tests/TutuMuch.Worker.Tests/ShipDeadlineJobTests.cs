// Unit Test
// Traces to: L2-033
// Description: Each pass of the ship deadline job sends one CancelUnshippedOrdersCommand in its own
// scope; the command (tested in ShipOrderTests) cancels and refunds Paid orders past their ship-by date.

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TutuMuch.Application.Orders;
using TutuMuch.Worker;

namespace TutuMuch.Worker.Tests;

public sealed class ShipDeadlineJobTests
{
    [Fact]
    public async Task A_pass_sends_the_cancel_command_and_returns_how_many_were_cancelled()
    {
        var sender = new RecordingSender(result: 2);
        using var services = new ServiceCollection().AddScoped<ISender>(_ => sender).BuildServiceProvider();
        var job = new ShipDeadlineJob(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OrderJobOptions()),
            NullLogger<ShipDeadlineJob>.Instance);

        var cancelled = await job.RunOnceAsync(CancellationToken.None);

        Assert.Equal(2, cancelled);
        Assert.IsType<CancelUnshippedOrdersCommand>(Assert.Single(sender.Sent));
    }

    [Fact]
    public void The_job_runs_at_least_every_15_minutes()
    {
        Assert.True(new OrderJobOptions().DeadlineJobInterval <= TimeSpan.FromMinutes(15));
    }
}
