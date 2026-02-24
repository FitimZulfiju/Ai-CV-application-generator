namespace AiCV.Infrastructure.Data;

public class DbInitializer(
    ApplicationDbContext context,
    UserManager<User> userManager,
    RoleManager<IdentityRole> roleManager,
    ILogger<DbInitializer> logger
) : IDbInitializer
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<User> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;
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
                    await _context.Database.MigrateAsync();
                }
                else
                {
                    _logger.LogInformation("No pending database migrations found.");
                }

                // Seed data
                await SeedAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }

    private async Task SeedAsync()
    {
        try
        {
            // Seed Roles
            if (!await _roleManager.RoleExistsAsync(Roles.Admin))
            {
                await _roleManager.CreateAsync(new IdentityRole(Roles.Admin));
                _logger.LogInformation("Created Admin role.");
            }

            if (!await _roleManager.RoleExistsAsync(Roles.User))
            {
                await _roleManager.CreateAsync(new IdentityRole(Roles.User));
                _logger.LogInformation("Created User role.");
            }

            // Seed User
            const string adminEmail = "demouser@aicv.com";
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                _logger.LogInformation("Seeding default admin user...");
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                };

                var result = await _userManager.CreateAsync(adminUser, "Demo123!");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, Roles.User);

                    // Seed Candidate Profile for Admin
                    var profile = DemoProfileData.GetSampleProfile();
                    profile.UserId = adminUser.Id;
                    profile.User = null; // Avoid circular reference issues during first save

                    _context.CandidateProfiles.Add(profile);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Admin user and demo profile seeded successfully.");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to seed admin user: {Errors}", errors);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}
