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
            int schemeEnd = databaseUrl.IndexOf("://", StringComparison.Ordinal);
            if (schemeEnd < 0)
            {
                return databaseUrl;
            }

            string remainder = databaseUrl.Substring(schemeEnd + 3);

            string username = "";
            string password = "";
            string hostDbPart = remainder;

            int lastAt = remainder.LastIndexOf('@');
            if (lastAt >= 0)
            {
                string userInfoPart = remainder.Substring(0, lastAt);
                hostDbPart = remainder.Substring(lastAt + 1);

                int firstColon = userInfoPart.IndexOf(':');
                if (firstColon >= 0)
                {
                    username = Uri.UnescapeDataString(userInfoPart.Substring(0, firstColon));
                    password = Uri.UnescapeDataString(userInfoPart.Substring(firstColon + 1));
                }
                else
                {
                    username = Uri.UnescapeDataString(userInfoPart);
                }
            }

            string hostPortPart = hostDbPart;
            string database = "postgres";

            int slash = hostDbPart.IndexOf('/');
            if (slash >= 0)
            {
                hostPortPart = hostDbPart.Substring(0, slash);
                database = hostDbPart.Substring(slash + 1);
            }

            // Strip query parameters or URL fragment from database name
            int qm = database.IndexOf('?');
            if (qm >= 0) database = database.Substring(0, qm);
            int hash = database.IndexOf('#');
            if (hash >= 0) database = database.Substring(0, hash);
            if (string.IsNullOrWhiteSpace(database)) database = "postgres";

            string host = hostPortPart;
            int port = 5432;

            if (hostPortPart.StartsWith("[") && hostPortPart.Contains(']'))
            {
                int closeBracket = hostPortPart.IndexOf(']');
                host = hostPortPart.Substring(1, closeBracket - 1);
                int colonAfter = hostPortPart.IndexOf(':', closeBracket);
                if (colonAfter >= 0 && int.TryParse(hostPortPart.Substring(colonAfter + 1), out int p6))
                {
                    port = p6;
                }
            }
            else
            {
                int colon = hostPortPart.LastIndexOf(':');
                if (colon >= 0 && int.TryParse(hostPortPart.Substring(colon + 1), out int p))
                {
                    host = hostPortPart.Substring(0, colon);
                    port = p;
                }
            }

            // Supabase IPv6-to-IPv4 auto-translation:
            // Direct endpoint 'db.[PROJECT_REF].supabase.co' is IPv6-only.
            // Cloud platforms without outbound IPv6 (like Render free tier) fail with 'Network is unreachable'.
            // Automatically reroute to the IPv4-compatible pooler.
            if (host.StartsWith("db.", StringComparison.OrdinalIgnoreCase) && 
                host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase))
            {
                int firstDot = host.IndexOf('.');
                int secondDot = host.IndexOf('.', firstDot + 1);
                if (firstDot >= 0 && secondDot > firstDot)
                {
                    string projectRef = host.Substring(firstDot + 1, secondDot - firstDot - 1);
                    string region = Environment.GetEnvironmentVariable("SUPABASE_REGION") ?? "ap-southeast-1";

                    host = $"aws-0-{region}.pooler.supabase.com";
                    if (!username.Contains('.'))
                    {
                        username = $"{username}.{projectRef}";
                    }
                    Console.WriteLine($"[INFO] Automatically routed Supabase direct IPv6 endpoint ({projectRef}) to IPv4 pooler '{host}' with user '{username}'.");
                }
            }

            return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Prefer;Trust Server Certificate=true;";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid DATABASE_URL format: '{databaseUrl}'. Error: {ex.Message}", ex);
        }
    }
}
