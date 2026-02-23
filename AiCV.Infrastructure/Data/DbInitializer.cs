using AiCV.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AiCV.Infrastructure.Data;

public class DbInitializer(
    ApplicationDbContext context,
    ILogger<DbInitializer> logger) : IDbInitializer
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<DbInitializer> _logger = logger;

    public async Task InitializeAsync()
    {
        try
        {
            if (_context.Database.IsSqlServer() || _context.Database.IsNpgsql())
            {
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                    await _context.Database.MigrateAsync();
                    _logger.LogInformation("Database migrations applied successfully.");
                }
                else
                {
                    _logger.LogInformation("No pending database migrations found.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }
}
