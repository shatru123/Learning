using System.Text.Json.Serialization;
using LearningOS.Common;
using LearningOS.Data;
using LearningOS.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add controllers with JSON settings
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Dynamic Render PORT support
var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

// Configure authoritative database
var (connectionString, isPostgreSql) = ConnectionStringHelper.ResolveConnectionString(builder.Configuration, builder.Environment);

bool isProduction = builder.Environment.IsProduction() || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RENDER"));
if (isProduction && !isPostgreSql)
{
    throw new InvalidOperationException("CRITICAL: Authoritative PostgreSQL is required in Production/Render. SQLite is strictly forbidden.");
}

builder.Services.AddDbContext<LearningDbContext>(options =>
{
    if (isPostgreSql)
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
        });
    }
    else
    {
        // Allowed ONLY in Development/Testing
        options.UseSqlite(connectionString);
    }
});

// Register Domain Services
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IStreakService, StreakService>();
builder.Services.AddScoped<IRecoveryService, RecoveryService>();
builder.Services.AddScoped<IBackupService, BackupService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.MapControllers();
app.MapFallbackToFile("index.html");

// Authoritative Startup Initialization & Migration
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<LearningDbContext>();

    try
    {
        logger.LogInformation("Connecting to authoritative database provider: {Provider}", db.Database.ProviderName);
        await DbInitializer.InitializeAsync(db, logger);
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "FATAL: Failed to initialize authoritative database. Service startup aborted.");
        if (app.Environment.IsProduction() || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RENDER")))
        {
            throw; // Fail fast in production!
        }
    }
}

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
