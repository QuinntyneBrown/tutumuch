// Unit Test
// Traces to: L2-028
// Description: Each pass of the reservation expiry job sends one ExpireReservationsCommand in its own scope.

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Checkouts;
using TuTuMuch.Worker;

namespace TuTuMuch.Worker.Tests;

public sealed class ReservationExpiryJobTests
{
    [Fact]
    public async Task A_pass_sends_the_expire_command_and_returns_how_many_ended()
    {
        var sender = new RecordingSender(result: 3);
        using var services = new ServiceCollection().AddScoped<ISender>(_ => sender).BuildServiceProvider();
        var job = new ReservationExpiryJob(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new CheckoutJobOptions()),
            NullLogger<ReservationExpiryJob>.Instance);

        var expired = await job.RunOnceAsync(CancellationToken.None);

        Assert.Equal(3, expired);
        Assert.IsType<ExpireReservationsCommand>(Assert.Single(sender.Sent));
    }

    [Fact]
    public void The_job_runs_well_inside_the_10_minute_reservation()
    {
        Assert.True(new CheckoutJobOptions().ExpiryJobInterval <= TimeSpan.FromSeconds(30));
    }
}
