using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TutuMuch.Application.Abstractions;
using TutuMuch.Infrastructure.Persistence;

namespace TutuMuch.Cli.Tests.Support;

/// <summary>
/// Runs TutuMuch.Cli against one SQLite in-memory database kept open for the harness's life, a fake
/// photo store and <see cref="EnsureCreatedSchema"/>. No SQL Server, LocalDB or live connection
/// string is used (AGENTS.md). The SQL Server connection string it passes is only printed.
/// </summary>
public sealed class CliHarness : IDisposable
{
    /// <summary>Printed as the reset target; never opened.</summary>
    public const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=TutuMuchDemo;User ID=demo;Password=not-a-real-secret;TrustServerCertificate=True";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CliHarness()
    {
        _connection.Open();
    }

    public FakePhotoStorage Photos { get; } = new();

    /// <summary>Extra settings for every run, such as Selling:PublishesPerDay.</summary>
    public Dictionary<string, string?> Settings { get; } = new();

    public Task<CliRun> RunAsync(string[] args, string environment = "Development", string? sqliteConnectionString = null) =>
        CliRun.RunAsync(args, ConnectionString, environment, Settings, services => Substitute(services, sqliteConnectionString));

    /// <summary>Sends one command or query through the same services a run uses, as the API would.</summary>
    public async Task<T> SendAsync<T>(IRequest<T> request)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(Settings) { ["ConnectionStrings:TutuMuch"] = ConnectionString })
            .Build();
        await using var services = CliApp.BuildServices(configuration, s => Substitute(s, null));
        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    /// <summary>Reads the database the runs wrote, through a fresh context.</summary>
    public T InDatabase<T>(Func<TutuMuchDbContext, T> query)
    {
        using var db = new TutuMuchDbContext(new DbContextOptionsBuilder<TutuMuchDbContext>().UseSqlite(_connection).Options);
        return query(db);
    }

    public void Dispose() => _connection.Dispose();

    private void Substitute(IServiceCollection services, string? sqliteConnectionString)
    {
        services.RemoveAll<DbContextOptions<TutuMuchDbContext>>();
        services.RemoveAll<IDbContextOptionsConfiguration<TutuMuchDbContext>>();
        services.AddDbContext<TutuMuchDbContext>(options =>
        {
            if (sqliteConnectionString is null)
            {
                options.UseSqlite(_connection);
            }
            else
            {
                options.UseSqlite(sqliteConnectionString);
            }
        });
        services.RemoveAll<IPhotoStorage>();
        services.AddSingleton<IPhotoStorage>(Photos);
        services.RemoveAll<IDatabaseSchema>();
        services.AddSingleton<IDatabaseSchema, EnsureCreatedSchema>();
    }
}
