namespace AiCV.Infrastructure.Services;

public class UserAIConfigurationService(
    ApplicationDbContext context,
    IDataProtectionProvider dataProtectionProvider
) : IUserAIConfigurationService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        "AiCV.AIConfigurations"
    );

    public async Task<List<UserAIConfiguration>> GetConfigurationsAsync(string userId)
    {
        var configs = await _context
            .UserAIConfigurations.Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IsActive) // Active first
            .ThenByDescending(c => c.Id)
            .ToListAsync();

        foreach (var config in configs)
        {
            config.ApiKey = Unprotect(config.ApiKey);
        }

        return configs;
    }

    public async Task<UserAIConfiguration?> GetActiveConfigurationAsync(string userId)
    {
        var config = await _context.UserAIConfigurations.FirstOrDefaultAsync(c =>
            c.UserId == userId && c.IsActive
        );

        if (config is null)
            return null;
        config.ApiKey = Unprotect(config.ApiKey);
        return config;
    }

    public async Task<UserAIConfiguration?> GetConfigurationAsync(int id, string userId)
    {
        var config = await _context.UserAIConfigurations.FirstOrDefaultAsync(c =>
            c.Id == id && c.UserId == userId
        );

        if (config is null)
            return null;
        config.ApiKey = Unprotect(config.ApiKey);
        return config;
    }

    public async Task<UserAIConfiguration> SaveConfigurationAsync(UserAIConfiguration config)
    {
        var apiKeyToProtect = config.ApiKey;
        // Avoid double protection if it's already encrypted?
        // Logic: Input config always comes with plain key from UI. Reading from DB comes with plain key (unprotected).
        // So we validly protect here.
        config.ApiKey = Protect(config.ApiKey);

        if (config.Id == 0)
        {
            // If first config, make it active
            if (!await _context.UserAIConfigurations.AnyAsync(c => c.UserId == config.UserId))
            {
                config.IsActive = true;
            }

            _context.UserAIConfigurations.Add(config);
        }
        else
        {
            var existing = await _context.UserAIConfigurations.FindAsync(config.Id);
            if (existing == null || existing.UserId != config.UserId)
            {
                throw new KeyNotFoundException("Configuration not found");
            }

            existing.Provider = config.Provider;
            existing.Name = config.Name;
            existing.ApiKey = config.ApiKey;
            existing.ModelId = config.ModelId;
            // IsActive is handled via ActivateConfigurationAsync or if it was already active

            _context.Entry(existing).State = EntityState.Modified;
        }

        await _context.SaveChangesAsync();

        // Return with unprotected key so UI doesn't break if it reuses the object
        config.ApiKey = apiKeyToProtect;
        return config;
    }

    public async Task<bool> DeleteConfigurationAsync(int id, string userId)
    {
        var config = await _context.UserAIConfigurations.FirstOrDefaultAsync(c =>
            c.Id == id && c.UserId == userId
        );

        if (config == null)
            return false;

        bool wasActive = config.IsActive;
        _context.UserAIConfigurations.Remove(config);
        await _context.SaveChangesAsync();

        if (wasActive)
        {
            // Activate another one if available
            var next = await _context
                .UserAIConfigurations.Where(c => c.UserId == userId)
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            if (next != null)
            {
                next.IsActive = true;
                await _context.SaveChangesAsync();
            }
        }

        return true;
    }

    public async Task<UserAIConfiguration?> ActivateConfigurationAsync(int id, string userId)
    {
        var config = await _context.UserAIConfigurations.FirstOrDefaultAsync(c =>
            c.Id == id && c.UserId == userId
        );

        if (config == null)
            return null;

        // Deactivate others
        var others = await _context
            .UserAIConfigurations.Where(c => c.UserId == userId && c.Id != id && c.IsActive)
            .ToListAsync();

        foreach (var other in others)
        {
            other.IsActive = false;
        }

        config.IsActive = true;
        await _context.SaveChangesAsync();

        config.ApiKey = Unprotect(config.ApiKey);
        return config;
    }

    private string? Protect(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        return _protector.Protect(input);
    }

    private string? Unprotect(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        try
        {
            return _protector.Unprotect(input);
        }
        catch
        {
            return string.Empty; // Fail safely
        }
    }
}
