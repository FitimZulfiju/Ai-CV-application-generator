namespace AiCV.Infrastructure.Services;

public class UserAIConfigurationService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IDataProtectionProvider dataProtectionProvider,
    IKeyManager keyManager,
    ILogger<UserAIConfigurationService> logger
) : IUserAIConfigurationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
    private readonly IKeyManager _keyManager = keyManager;
    private readonly ILogger<UserAIConfigurationService> _logger = logger;
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
        "AiCV.AIConfigurations"
    );

    public async Task<List<UserAIConfiguration>> GetConfigurationsAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var configs = await context
            .UserAIConfigurations.AsNoTracking()
            .Where(c => c.UserId == userId)
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
        await using var context = await _contextFactory.CreateDbContextAsync();
        var config = await context.UserAIConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.UserId == userId && c.IsActive
            );

        if (config is null)
            return null;
        config.ApiKey = Unprotect(config.ApiKey);
        return config;
    }

    public async Task<UserAIConfiguration?> GetConfigurationAsync(int id, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var config = await context.UserAIConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == id && c.UserId == userId
            );

        if (config is null)
            return null;
        config.ApiKey = Unprotect(config.ApiKey);
        return config;
    }

    public async Task<UserAIConfiguration> SaveConfigurationAsync(UserAIConfiguration config)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var apiKeyToProtect = config.ApiKey;

        config.ApiKey = Protect(config.ApiKey);

        if (config.Id == 0)
        {
            // If first config, make it active
            if (!await context.UserAIConfigurations.AnyAsync(c => c.UserId == config.UserId))
            {
                config.IsActive = true;
            }

            context.UserAIConfigurations.Add(config);
        }
        else
        {
            var existing = await context.UserAIConfigurations.FindAsync(config.Id);
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
            existing.BaseUrl = config.BaseUrl;

            context.Entry(existing).State = EntityState.Modified;
        }

        await context.SaveChangesAsync();

        config.ApiKey = apiKeyToProtect;
        return config;
    }

    public async Task<bool> DeleteConfigurationAsync(int id, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var config = await context.UserAIConfigurations.FirstOrDefaultAsync(c =>
            c.Id == id && c.UserId == userId
        );

        if (config == null)
            return false;

        bool wasActive = config.IsActive;
        context.UserAIConfigurations.Remove(config);
        await context.SaveChangesAsync();

        if (wasActive)
        {
            var next = await context
                .UserAIConfigurations.Where(c => c.UserId == userId)
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            if (next != null)
            {
                next.IsActive = true;
                await context.SaveChangesAsync();
            }
        }

        return true;
    }

    public async Task<UserAIConfiguration?> ActivateConfigurationAsync(int id, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var config = await context.UserAIConfigurations.FirstOrDefaultAsync(c =>
            c.Id == id && c.UserId == userId
        );

        if (config == null)
            return null;

        var others = await context
            .UserAIConfigurations.Where(c => c.UserId == userId && c.Id != id && c.IsActive)
            .ToListAsync();

        foreach (var other in others)
        {
            other.IsActive = false;
        }

        config.IsActive = true;
        await context.SaveChangesAsync();

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

        if (!TryDecodeProtectedPayload(input, out var protectedBytes, out var keyId))
        {
            return input;
        }

        if (!_keyManager.GetAllKeys().Any(key => key.KeyId == keyId))
        {
            _logger.LogWarning(
                "Stored AI configuration API key was protected with missing Data Protection key {KeyId}.",
                keyId
            );
            return "DECRYPTION_FAILED";
        }

        if (_protector is IPersistedDataProtector persistedProtector)
        {
            try
            {
                var result = persistedProtector.DangerousUnprotect(
                    protectedBytes,
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
                _logger.LogWarning(ex2, "Could not decrypt stored AI configuration API key.");
            }
        }
        else
        {
            try
            {
                return _protector.Unprotect(input);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not decrypt stored AI configuration API key.");
            }
        }

        return "DECRYPTION_FAILED";
    }

    private static bool TryDecodeProtectedPayload(
        string input,
        out byte[] protectedBytes,
        out Guid keyId
    )
    {
        protectedBytes = [];
        keyId = Guid.Empty;

        if (input.StartsWith("oauth_refresh:", StringComparison.Ordinal))
        {
            return false;
        }

        var normalized = input.Replace('-', '+').Replace('_', '/');
        var padding = normalized.Length % 4;
        if (padding == 1)
        {
            return false;
        }

        if (padding > 0)
        {
            normalized = normalized.PadRight(normalized.Length + 4 - padding, '=');
        }

        protectedBytes = new byte[normalized.Length];
        if (!Convert.TryFromBase64String(normalized, protectedBytes, out var bytesWritten))
        {
            protectedBytes = [];
            return false;
        }

        Array.Resize(ref protectedBytes, bytesWritten);
        if (
            protectedBytes.Length < 20
            || protectedBytes[0] != 0x09
            || protectedBytes[1] != 0xF0
            || protectedBytes[2] != 0xC9
            || protectedBytes[3] != 0xF0
        )
        {
            protectedBytes = [];
            return false;
        }

        keyId = new Guid(protectedBytes.AsSpan(4, 16));
        return true;
    }
}
