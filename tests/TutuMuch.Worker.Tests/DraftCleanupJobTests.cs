// Unit Test
// Traces to: L2-019
// Description: Each pass of the draft clean-up job sends one CleanUpDraftsCommand in its own scope;
// the command is tested in DraftCleanupTests. The job runs at least once a day.

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TutuMuch.Application.Listings;
using TutuMuch.Worker;

namespace TutuMuch.Worker.Tests;

public sealed class DraftCleanupJobTests
{
    [Fact]
    public async Task A_pass_sends_the_clean_up_command_and_returns_its_count()
    {
        var sender = new RecordingSender(result: 2);
        using var services = new ServiceCollection().AddScoped<ISender>(_ => sender).BuildServiceProvider();
        var job = new DraftCleanupJob(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DraftCleanupJobOptions()),
            NullLogger<DraftCleanupJob>.Instance);

        Assert.Equal(2, await job.RunOnceAsync(CancellationToken.None));
        Assert.IsType<CleanUpDraftsCommand>(Assert.Single(sender.Sent));
    }

    [Fact]
    public void The_job_runs_at_least_daily()
    {
        Assert.True(new DraftCleanupJobOptions().Interval <= TimeSpan.FromDays(1));
    }
}
