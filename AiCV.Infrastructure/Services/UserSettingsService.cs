namespace AiCV.Infrastructure.Services;

public class UserSettingsService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IDataProtectionProvider dataProtectionProvider,
    IKeyManager keyManager,
    ILogger<UserSettingsService> logger
    ) : IUserSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
    private readonly IKeyManager _keyManager = keyManager;
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
        if (!TryDecodeProtectedPayload(cipherText, out var protectedBytes, out var keyId))
        {
            return cipherText;
        }

        if (!_keyManager.GetAllKeys().Any(key => key.KeyId == keyId))
        {
            _logger.LogWarning(
                "Stored user settings API key was protected with missing Data Protection key {KeyId}.",
                keyId
            );
            return string.Empty;
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
                _logger.LogWarning(ex2, "Could not decrypt stored user settings API key.");
            }
        }
        else
        {
            try
            {
                return _protector.Unprotect(cipherText);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not decrypt stored user settings API key.");
            }
        }

        return string.Empty;
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
