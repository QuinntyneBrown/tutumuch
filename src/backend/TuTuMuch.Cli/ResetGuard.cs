using Microsoft.Data.SqlClient;

namespace TuTuMuch.Cli;

/// <summary>Keeps reset to a developer's own database (L2-073).</summary>
public static class ResetGuard
{
    public static bool Allows(string environmentName, bool force) =>
        force || string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);

    /// <summary>The server and database a connection string names, without any credential.</summary>
    public static string DescribeTarget(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var server = string.IsNullOrEmpty(builder.DataSource) ? "(not set)" : builder.DataSource;
            var database = string.IsNullOrEmpty(builder.InitialCatalog) ? "(not set)" : builder.InitialCatalog;
            return $"Target: server {server}, database {database}";
        }
        catch (ArgumentException)
        {
            return "Target: (the connection string could not be read)";
        }
    }
}
