// Unit Test
// Traces to: L2-031
// Description: Each pass of the payout release job sends one ReleasePayoutsCommand in its own
// scope; the command (tested in PayoutTests) sends only payouts of Completed orders.

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TutuMuch.Application.Payouts;
using TutuMuch.Worker;

namespace TutuMuch.Worker.Tests;

public sealed class PayoutReleaseJobTests
{
    [Fact]
    public async Task A_pass_sends_the_release_command_and_returns_how_many_were_sent()
    {
        var sender = new RecordingSender(result: 2);
        using var services = new ServiceCollection().AddScoped<ISender>(_ => sender).BuildServiceProvider();
        var job = new PayoutReleaseJob(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new PayoutJobOptions()),
            NullLogger<PayoutReleaseJob>.Instance);

        var sent = await job.RunOnceAsync(CancellationToken.None);

        Assert.Equal(2, sent);
        Assert.IsType<ReleasePayoutsCommand>(Assert.Single(sender.Sent));
    }
}
