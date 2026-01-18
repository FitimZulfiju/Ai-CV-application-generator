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
            ?? "Host=localhost;Database=aicv_db;Username=sa;Password=postgres;";

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
