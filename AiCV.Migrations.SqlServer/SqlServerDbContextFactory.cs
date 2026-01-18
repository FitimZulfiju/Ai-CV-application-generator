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
            "Server=localhost;Database=AiCV_db;Trusted_Connection=True;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(
            connectionString,
            sql =>
            {
                sql.MigrationsAssembly(typeof(SqlServerDbContextFactory).Assembly.GetName().Name);
            }
        );

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
