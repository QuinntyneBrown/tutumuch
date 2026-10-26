// Unit Test
// Traces to: L2-042, L2-033
// Description: Each pass of the email dispatch job sends one DispatchEmailsCommand, and each pass of
// the ship reminder job one SendShipRemindersCommand, in its own scope; the commands are tested in
// EmailNotificationTests. The dispatcher polls often enough to send within 2 minutes.

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Notifications;
using TuTuMuch.Application.Orders;
using TuTuMuch.Worker;

namespace TuTuMuch.Worker.Tests;

public sealed class EmailJobsTests
{
    [Fact]
    public async Task A_dispatch_pass_sends_the_dispatch_command_and_returns_how_many_were_sent()
    {
        var sender = new RecordingSender(result: 3);
        using var services = new ServiceCollection().AddScoped<ISender>(_ => sender).BuildServiceProvider();
        var job = new EmailDispatchJob(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new EmailDispatchJobOptions()),
            NullLogger<EmailDispatchJob>.Instance);

        Assert.Equal(3, await job.RunOnceAsync(CancellationToken.None));
        Assert.IsType<DispatchEmailsCommand>(Assert.Single(sender.Sent));
    }

    [Fact]
    public async Task A_reminder_pass_sends_the_reminder_command()
    {
        var sender = new RecordingSender(result: 1);
        using var services = new ServiceCollection().AddScoped<ISender>(_ => sender).BuildServiceProvider();
        var job = new ShipReminderJob(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new EmailDispatchJobOptions()),
            NullLogger<ShipReminderJob>.Instance);

        Assert.Equal(1, await job.RunOnceAsync(CancellationToken.None));
        Assert.IsType<SendShipRemindersCommand>(Assert.Single(sender.Sent));
    }

    [Fact]
    public void The_dispatcher_polls_well_inside_the_2_minute_delivery_time()
    {
        Assert.True(new EmailDispatchJobOptions().PollInterval <= TimeSpan.FromSeconds(30));
        Assert.True(new EmailDispatchJobOptions().ShipReminderJobInterval <= TimeSpan.FromHours(1));
    }
}
