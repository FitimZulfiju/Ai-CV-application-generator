namespace AiCV.Infrastructure.Services;

public class UserSettingsService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IDataProtectionProvider dataProtectionProvider,
    ILogger<UserSettingsService> logger
    ) : IUserSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
    private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(
            "AiCV.Infrastructure.Services.UserSettingsService"
        );

    private readonly ILogger<UserSettingsService> _logger = logger;

    public async Task<UserSettings?> GetUserSettingsAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var settings = await context
            .UserSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            return null;
        }

        // Decrypt keys for usage
        if (!string.IsNullOrEmpty(settings.OpenAIApiKey))
        {
            settings.OpenAIApiKey = Decrypt(settings.OpenAIApiKey);
        }

        if (!string.IsNullOrEmpty(settings.GoogleGeminiApiKey))
        {
            settings.GoogleGeminiApiKey = Decrypt(settings.GoogleGeminiApiKey);
        }

        if (!string.IsNullOrEmpty(settings.ClaudeApiKey))
        {
            settings.ClaudeApiKey = Decrypt(settings.ClaudeApiKey);
        }

        if (!string.IsNullOrEmpty(settings.GroqApiKey))
        {
            settings.GroqApiKey = Decrypt(settings.GroqApiKey);
        }

        if (!string.IsNullOrEmpty(settings.DeepSeekApiKey))
        {
            settings.DeepSeekApiKey = Decrypt(settings.DeepSeekApiKey);
        }

        if (!string.IsNullOrEmpty(settings.OpenRouterApiKey))
        {
            settings.OpenRouterApiKey = Decrypt(settings.OpenRouterApiKey);
        }

        return settings;
    }

    public async Task SaveUserSettingsAsync(
        string userId,
        string? openAiApiKey,
        string? googleGeminiApiKey,
        string? claudeApiKey,
        string? groqApiKey,
        string? deepSeekApiKey,
        string? openRouterApiKey,
        AIProvider defaultProvider,
        string? defaultModelId
    )
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var settings = await context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            var userExists = await context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                _logger.LogWarning("User {UserId} not found. Cannot save settings.", userId);
                throw new InvalidOperationException("User not found.");
            }

            settings = new UserSettings { UserId = userId, CreatedDate = DateTime.UtcNow };
            context.UserSettings.Add(settings);
        }

        settings.OpenAIApiKey = !string.IsNullOrEmpty(openAiApiKey) ? Encrypt(openAiApiKey) : null;
        settings.GoogleGeminiApiKey = !string.IsNullOrEmpty(googleGeminiApiKey)
            ? Encrypt(googleGeminiApiKey)
            : null;
        settings.ClaudeApiKey = !string.IsNullOrEmpty(claudeApiKey) ? Encrypt(claudeApiKey) : null;
        settings.GroqApiKey = !string.IsNullOrEmpty(groqApiKey) ? Encrypt(groqApiKey) : null;
        settings.DeepSeekApiKey = !string.IsNullOrEmpty(deepSeekApiKey)
            ? Encrypt(deepSeekApiKey)
            : null;
        settings.OpenRouterApiKey = !string.IsNullOrEmpty(openRouterApiKey)
            ? Encrypt(openRouterApiKey)
            : null;

        settings.DefaultProvider = defaultProvider;
        settings.DefaultModelId = defaultModelId;
        settings.UpdatedDate = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    private string Encrypt(string clearText)
    {
        try
        {
            return _protector.Protect(clearText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption failed");
            return string.Empty;
        }
    }

    private string Decrypt(string cipherText)
    {
        try
        {
            return _protector.Unprotect(cipherText);
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
                    Convert.FromBase64String(cipherText),
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
        return cipherText;
    }
}
