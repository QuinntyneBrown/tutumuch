// Unit Test
// Traces to: L2-098
// Description: Each pass of the item request clean-up job sends one CleanUpItemRequestsCommand in its own scope;
// the command is tested in ItemRequestMatchingTests. The job runs at least once a day.

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Requests;
using TuTuMuch.Worker;

namespace TuTuMuch.Worker.Tests;

public sealed class ItemRequestCleanupJobTests
{
    [Fact]
    public async Task A_pass_sends_the_clean_up_command_and_returns_its_count()
    {
        var sender = new RecordingSender(result: 2);
        using var services = new ServiceCollection().AddScoped<ISender>(_ => sender).BuildServiceProvider();
        var job = new ItemRequestCleanupJob(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new ItemRequestCleanupJobOptions()),
            NullLogger<ItemRequestCleanupJob>.Instance);

        Assert.Equal(2, await job.RunOnceAsync(CancellationToken.None));
        Assert.IsType<CleanUpItemRequestsCommand>(Assert.Single(sender.Sent));
    }

    [Fact]
    public void The_job_runs_at_least_daily()
    {
        Assert.True(new ItemRequestCleanupJobOptions().Interval <= TimeSpan.FromDays(1));
    }
}
