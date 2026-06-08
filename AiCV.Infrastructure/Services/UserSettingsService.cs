namespace AiCV.Infrastructure.Services;

using Microsoft.AspNetCore.DataProtection;

public class UserSettingsService : IUserSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IDataProtector _protector;

    private readonly ILogger<UserSettingsService> _logger;

    public UserSettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<UserSettingsService> logger
    )
    {
        _contextFactory = contextFactory;
        _protector = dataProtectionProvider.CreateProtector(
            "AiCV.Infrastructure.Services.UserSettingsService"
        );
        _logger = logger;
    }

    public async Task<UserSettings?> GetUserSettingsAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        _logger.LogInformation("Getting settings for userId: {UserId}", userId);
        var settings = await context
            .UserSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            _logger.LogInformation("No settings found for userId: {UserId}", userId);
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

        _logger.LogInformation("Settings retrieved for userId: {UserId}", userId);
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
        _logger.LogInformation("Saving settings for userId: {UserId}", userId);
        var settings = await context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

        if (settings == null)
        {
            // Check if user actually exists to avoid FK error
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
        _logger.LogInformation("Settings saved to database for userId: {UserId}", userId);
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
            _logger.LogError(ex, "Decryption failed");
            return string.Empty;
        }
    }
}
