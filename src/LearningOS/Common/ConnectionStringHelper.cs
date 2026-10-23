namespace LearningOS.Common;

public static class ConnectionStringHelper
{
    public static (string ConnectionString, bool IsPostgreSql) ResolveConnectionString(IConfiguration configuration, IHostEnvironment environment)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") 
                       ?? configuration["DATABASE_URL"];

        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            if (databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return (ConvertPostgresUrlToNpgsql(databaseUrl), true);
            }
            return (databaseUrl, true);
        }

        bool isProduction = environment.IsProduction() || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RENDER"));

        var configured = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            bool isPg = configured.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
                        configured.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
                        configured.Contains("Port=", StringComparison.OrdinalIgnoreCase) ||
                        configured.Contains("Username=", StringComparison.OrdinalIgnoreCase);

            if (isProduction && !isPg)
            {
                Console.WriteLine($"[WARNING] Configured connection string '{configured}' is not PostgreSQL. Using SQLite fallback.");
            }

            return (configured, isPg);
        }

        if (isProduction)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("[WARNING] Running on SQLite fallback in Production/Render environment.");
            Console.WriteLine("Neither DATABASE_URL nor ConnectionStrings__DefaultConnection was supplied.");
            Console.WriteLine("The app will store data locally in SQLite (learningos_production.db).");
            Console.WriteLine("For permanent persistent data across restarts, attach a PostgreSQL database URL.");
            Console.WriteLine("================================================================================");

            return ("Data Source=learningos_production.db", false);
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
