namespace AiCV.Migrations.PostgreSQL;

/// <summary>
/// Design-time factory for EF Core migrations targeting PostgreSQL.
/// Used by 'dotnet ef migrations add' and 'dotnet ef database update' commands.
/// Reads connection string from environment variable or uses default.
/// </summary>
public class PostgreSqlDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Try to get connection string from environment variable first
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

        if (string.IsNullOrEmpty(connectionString))
        {
            var host = Environment.GetEnvironmentVariable("PG_HOST") ?? "localhost";
            var port = Environment.GetEnvironmentVariable("PG_PORT") ?? "5432";
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "aicv_db";
            var user = Environment.GetEnvironmentVariable("DB_USER") ?? "sa";
            var pass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";

            connectionString =
                $"Host={host};Port={port};Database={dbName};Username={user};Password={pass};";
        }

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsAssembly(
                    typeof(PostgreSqlDbContextFactory).Assembly.GetName().Name
                );
            }
        );

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
