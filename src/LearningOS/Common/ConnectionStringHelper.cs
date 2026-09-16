namespace LearningOS.Common;

public static class ConnectionStringHelper
{
    public static (string ConnectionString, bool IsPostgreSql) ResolveConnectionString(IConfiguration configuration, IHostEnvironment environment)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") 
                       ?? configuration["DATABASE_URL"];

        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return (ConvertPostgresUrlToNpgsql(databaseUrl), true);
        }

        var configured = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            bool isPg = configured.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
                        configured.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
                        configured.Contains("Port=", StringComparison.OrdinalIgnoreCase);
            return (configured, isPg);
        }

        bool isProduction = environment.IsProduction() || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RENDER"));

        if (isProduction)
        {
            throw new InvalidOperationException(
                "CRITICAL ERROR: Authoritative PostgreSQL database connection is REQUIRED in Production/Render environment. " +
                "Neither DATABASE_URL nor ConnectionStrings__DefaultConnection was supplied. " +
                "Silent fallback to in-memory, JSON, or SQLite is strictly prohibited.");
        }

        // Local Development offline fallback
        return ("Data Source=learningos_dev.db", false);
    }

    public static string ConvertPostgresUrlToNpgsql(string databaseUrl)
    {
        // Format: postgres://user:password@host:port/database
        // or postgresql://user:password@host:port/database
        try
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var database = uri.AbsolutePath.TrimStart('/');
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;

            return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true;";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid DATABASE_URL format: '{databaseUrl}'. Error: {ex.Message}", ex);
        }
    }
}
