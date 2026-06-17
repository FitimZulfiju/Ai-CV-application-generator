namespace AiCV.Infrastructure.Services;

public class UserAIConfigurationService(
ApplicationDbContext context,
IDataProtectionProvider dataProtectionProvider,
ILogger<UserAIConfigurationService> logger
) : IUserAIConfigurationService
{
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<UserAIConfigurationService> _logger = logger;
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
            existing.CostType = config.CostType;
            existing.Notes = config.Notes;

            _context.Entry(existing).State = EntityState.Modified;
        }

        await _context.SaveChangesAsync();

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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Standard Unprotect failed, attempting DangerousUnprotect fallback.");
        }

        if (_protector is IPersistedDataProtector persistedProtector)
        {
            try
            {
                var result = persistedProtector.DangerousUnprotect(
                    Convert.FromBase64String(input),
                    ignoreRevocationErrors: true,
                    out bool requiresMigration,
                    out bool wasRevoked
                );

                var decrypted = Encoding.UTF8.GetString(result);

                if (wasRevoked || requiresMigration)
                {
                    _logger.LogWarning(
                        "API key was decrypted with DangerousUnprotect (wasRevoked={WasRevoked}, requiresMigration={RequiresMigration}). Key should be re-saved to use current protection keys.",
                        wasRevoked, requiresMigration
                    );
                }

                return decrypted;
            }
            catch (Exception ex2)
            {
                _logger.LogWarning(ex2, "DangerousUnprotect also failed. Returning raw value as fallback.");
            }
        }
        else
        {
            _logger.LogWarning("IPersistedDataProtector not available; skipping DangerousUnprotect tier.");
        }

        _logger.LogWarning("All decryption attempts failed. Returning raw stored value as API key.");
        return input;
    }
}
