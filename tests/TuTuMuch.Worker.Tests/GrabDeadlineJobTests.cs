// Unit Test
// Traces to: L2-119
// Description: Each pass of the grab deadline job sends one RunGrabDeadlinesCommand in its own scope;
// the command is tested in GrabsTests. The job runs at least every 5 minutes so a missed deadline is
// refunded promptly.

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Grabs;
using TuTuMuch.Worker;

namespace TuTuMuch.Worker.Tests;

public sealed class GrabDeadlineJobTests
{
    [Fact]
    public async Task A_pass_sends_the_deadline_command_and_returns_its_result()
    {
        var sender = new DeadlineSender();
        using var services = new ServiceCollection().AddScoped<ISender>(_ => sender).BuildServiceProvider();
        var job = new GrabDeadlineJob(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new GrabDeadlineJobOptions()),
            NullLogger<GrabDeadlineJob>.Instance);

        Assert.Equal(new GrabDeadlineResult(2, 1), await job.RunOnceAsync(CancellationToken.None));
        Assert.IsType<RunGrabDeadlinesCommand>(Assert.Single(sender.Sent));
    }

    [Fact]
    public void The_job_runs_at_least_every_5_minutes()
    {
        Assert.True(new GrabDeadlineJobOptions().Interval <= TimeSpan.FromMinutes(5));
    }

    private sealed class DeadlineSender : ISender
    {
        public List<object> Sent { get; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            Sent.Add(request);
            return Task.FromResult((TResponse)(object)new GrabDeadlineResult(2, 1));
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest => throw new NotSupportedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
