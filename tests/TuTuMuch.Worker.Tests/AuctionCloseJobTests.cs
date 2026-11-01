// Unit Test
// Traces to: L2-117, L2-118
// Description: Each pass of the auction close job sends one CloseAuctionsCommand in its own scope,
// and the job runs at least once a minute.

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Listings;
using TuTuMuch.Worker;

namespace TuTuMuch.Worker.Tests;

public sealed class AuctionCloseJobTests
{
    [Fact]
    public async Task A_pass_sends_the_close_command_and_returns_how_many_changed()
    {
        var sender = new RecordingSender(result: 2);
        using var services = new ServiceCollection().AddScoped<ISender>(_ => sender).BuildServiceProvider();
        var job = new AuctionCloseJob(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new AuctionJobOptions()),
            NullLogger<AuctionCloseJob>.Instance);

        var closed = await job.RunOnceAsync(CancellationToken.None);

        Assert.Equal(2, closed);
        Assert.IsType<CloseAuctionsCommand>(Assert.Single(sender.Sent));
    }

    [Fact]
    public void The_job_runs_at_least_once_a_minute()
    {
        Assert.True(new AuctionJobOptions().CloseJobInterval <= TimeSpan.FromMinutes(1));
    }
}
