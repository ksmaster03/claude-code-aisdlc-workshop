using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using OpsTrack.Api.Errors;
using OpsTrack.Api.Security;
using OpsTrack.Business.Common;
using OpsTrack.Business.Lookups;
using OpsTrack.Business.Notifications;
using OpsTrack.Business.Numbering;
using OpsTrack.Business.Reports;
using OpsTrack.Business.Shifts;
using OpsTrack.Business.Sla;
using OpsTrack.Business.WorkRequests;
using OpsTrack.Data;
using OpsTrack.Data.Seed;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    // legacy: match plant server locale (reports and labels were built for Thai regional settings)
    var plantCulture = new CultureInfo("th-TH");
    CultureInfo.DefaultThreadCurrentCulture = plantCulture;
    CultureInfo.DefaultThreadCurrentUICulture = plantCulture;
}

// Data access
builder.Services.AddDbContext<OpsTrackDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("OpsTrack"));

    // Turned on only while troubleshooting: writes SQL parameter values to the log.
    if (builder.Configuration.GetValue<bool>("Diagnostics:LogSqlParameters"))
    {
        options.EnableSensitiveDataLogging();
    }
});

// Business services
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<SlaCalculator>();
builder.Services.AddSingleton<ShiftCalendar>();
builder.Services.AddScoped<RunningNumberService>();
builder.Services.AddScoped<WorkRequestService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<EquipmentService>();

builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection("Notification"));
builder.Services.AddHttpClient<INotificationClient, NotificationClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Web
builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<OpsTrackExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseMiddleware<LabUserMiddleware>();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OpsTrackDbContext>();
    if (app.Configuration.GetValue<bool>("Database:ResetOnStartup"))
    {
        db.Database.EnsureDeleted();
    }

    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

app.Run();

// Lets WebApplicationFactory<Program> find this assembly in the integration tests.
public partial class Program
{
}
