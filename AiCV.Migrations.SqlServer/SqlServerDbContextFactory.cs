namespace AiCV.Migrations.SqlServer;

/// <summary>
/// Design-time factory for EF Core migrations targeting SQL Server.
/// Used by 'dotnet ef migrations add' command.
/// </summary>
public class SqlServerDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Default connection string for design-time migrations
        // This is only used when running 'dotnet ef migrations add'
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

        if (string.IsNullOrEmpty(connectionString))
        {
            var server = Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost";
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "aicv_db"; // Match .env default
            var user = Environment.GetEnvironmentVariable("DB_USER");
            var pass = Environment.GetEnvironmentVariable("DB_PASSWORD");

            if (!string.IsNullOrEmpty(user))
            {
                connectionString =
                    $"Server={server};Database={dbName};User Id={user};Password={pass};TrustServerCertificate=True;MultipleActiveResultSets=true;";
            }
            else
            {
                connectionString =
                    $"Server={server};Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
            }
        }

        optionsBuilder.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(typeof(SqlServerDbContextFactory).Assembly.GetName().Name)
        );

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
