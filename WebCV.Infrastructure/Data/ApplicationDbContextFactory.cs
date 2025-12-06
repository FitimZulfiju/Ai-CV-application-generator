namespace WebCV.Infrastructure.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use a dummy connection string for design-time operations
        // The actual connection string comes from appsettings.json at runtime
        optionsBuilder.UseSqlServer("Server=localhost;Database=WebCV_DesignTime;Trusted_Connection=True;TrustServerCertificate=True;");
        
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
