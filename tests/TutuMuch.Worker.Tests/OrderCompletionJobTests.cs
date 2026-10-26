// Unit Test
// Traces to: L2-034
// Description: Each pass of the order completion job sends one CompleteDeliveredOrdersCommand in
// its own scope; the command (tested in CompleteOrderTests) completes Delivered orders 3 days after delivery.

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TutuMuch.Application.Orders;
using TutuMuch.Worker;

namespace TutuMuch.Worker.Tests;

public sealed class OrderCompletionJobTests
{
    [Fact]
    public async Task A_pass_sends_the_complete_command_and_returns_how_many_were_completed()
    {
        var sender = new RecordingSender(result: 3);
        using var services = new ServiceCollection().AddScoped<ISender>(_ => sender).BuildServiceProvider();
        var job = new OrderCompletionJob(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OrderJobOptions()),
            NullLogger<OrderCompletionJob>.Instance);

        var completed = await job.RunOnceAsync(CancellationToken.None);

        Assert.Equal(3, completed);
        Assert.IsType<CompleteDeliveredOrdersCommand>(Assert.Single(sender.Sent));
    }

    [Fact]
    public void The_job_runs_at_least_every_15_minutes()
    {
        Assert.True(new OrderJobOptions().CompletionJobInterval <= TimeSpan.FromMinutes(15));
    }
}
