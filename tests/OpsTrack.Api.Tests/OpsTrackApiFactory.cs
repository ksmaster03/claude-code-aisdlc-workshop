using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpsTrack.Business.Common;
using OpsTrack.Business.Notifications;
using OpsTrack.Data;

namespace OpsTrack.Api.Tests;

/// <summary>
/// Starts the real API in memory with:
///  - one shared, open SQLite in-memory connection (the database lives as long as the factory),
///  - a fixed clock (2026-11-13 10:00 plant time),
///  - the notification webhook replaced by <see cref="StubHttpMessageHandler"/> so no real HTTP call leaves the test.
/// </summary>
public sealed class OpsTrackApiFactory : WebApplicationFactory<Program>
{
    public const string TestWebhookUrl = "https://notify.test.invalid/sla";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public OpsTrackApiFactory()
    {
        _connection.Open();
    }

    public StubHttpMessageHandler Notifications { get; } = new();

    public FakeClock Clock { get; } = new(new DateTime(2026, 11, 13, 10, 0, 0));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Notification:WebhookUrl"] = TestWebhookUrl,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<OpsTrackDbContext>>();
            services.AddDbContext<OpsTrackDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Clock);

            // Same typed client as Program.cs, but the HTTP handler is the stub.
            services.AddHttpClient<INotificationClient, NotificationClient>()
                .ConfigurePrimaryHttpMessageHandler(() => Notifications);
        });
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

public sealed class FakeClock : IClock
{
    public FakeClock(DateTime now)
    {
        Now = now;
    }

    public DateTime Now { get; set; }
}
