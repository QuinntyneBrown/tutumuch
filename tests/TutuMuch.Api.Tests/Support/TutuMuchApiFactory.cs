using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using TutuMuch.Application.Abstractions;
using TutuMuch.Application.Notifications;
using TutuMuch.Application.Orders;
using TutuMuch.Infrastructure.Persistence;

namespace TutuMuch.Api.Tests.Support;

/// <summary>
/// Hosts TutuMuch.Api in memory with SQLite in-memory in place of SQL Server, a captured email
/// sender, in-memory photo storage, a fake payment provider, a fake carrier tracking service, a fake payout provider and a controllable clock. No real database or external system is touched (AGENTS.md).
/// </summary>
public sealed class TutuMuchApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;
    private readonly string? _sharedDatabase;

    /// <summary>
    /// One in-memory database on one connection. With <paramref name="concurrent"/>, every
    /// DbContext opens its own connection to a shared-cache in-memory database instead, so
    /// parallel requests run in parallel as they would against SQL Server.
    /// </summary>
    public TutuMuchApiFactory(bool concurrent = false)
    {
        _sharedDatabase = concurrent ? $"Data Source=file:tutumuch-{Guid.NewGuid():N}?mode=memory&cache=shared" : null;
        _connection = new SqliteConnection(_sharedDatabase ?? "DataSource=:memory:");
        _connection.Open();
    }

    /// <summary>Starts at the real time, because the cookie container expires cookies by the real clock.</summary>
    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow);

    public CapturingEmailSender Email { get; } = new();

    public FakePhotoStorage Photos { get; } = new();

    public FakePaymentProvider Payments { get; } = new();

    public FakeCarrierTracking Carriers { get; } = new();

    public FakePayoutProvider PayoutProvider { get; } = new();

    public RecordingOrderCompleted Completed { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Auth:SigningKey", "test-signing-key-that-is-at-least-32-bytes-long");
        builder.UseSetting("App:BaseUrl", "https://tutumuch.test");
        builder.UseSetting("Notifications:UnsubscribeSigningKey", "test-unsubscribe-key");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<TutuMuchDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TutuMuchDbContext>>();
            services.AddDbContext<TutuMuchDbContext>(options =>
            {
                if (_sharedDatabase is null)
                {
                    options.UseSqlite(_connection);
                }
                else
                {
                    options.UseSqlite(_sharedDatabase);
                }
            });
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Email);
            services.RemoveAll<IPhotoStorage>();
            services.AddSingleton<IPhotoStorage>(Photos);
            services.RemoveAll<IPaymentProvider>();
            services.AddSingleton<IPaymentProvider>(Payments);
            services.RemoveAll<ICarrierTracking>();
            services.AddSingleton<ICarrierTracking>(Carriers);
            services.RemoveAll<IPayoutProvider>();
            services.AddSingleton<IPayoutProvider>(PayoutProvider);
            services.AddSingleton<INotificationHandler<OrderCompleted>>(Completed);
        });
    }

    /// <summary>Creates the schema once the host is built.</summary>
    public void EnsureDatabase()
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<TutuMuchDbContext>().Database.EnsureCreated();
    }

    /// <summary>An HTTPS client that keeps cookies, so Secure session cookies round-trip.</summary>
    public HttpClient CreateMemberClient()
    {
        EnsureDatabase();
        return CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    }

    /// <summary>One dispatcher pass, as the worker runs it; returns the emails it sent.</summary>
    public async Task<IReadOnlyList<EmailMessage>> DispatchEmailsAsync()
    {
        var before = Email.Sent.Count;
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(new DispatchEmailsCommand());
        return Email.Sent.Skip(before).ToList();
    }

    public void InDatabase(Action<TutuMuchDbContext> action)
    {
        using var scope = Services.CreateScope();
        action(scope.ServiceProvider.GetRequiredService<TutuMuchDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
