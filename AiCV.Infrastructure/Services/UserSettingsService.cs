namespace AiCV.Infrastructure.Services
{
    public class UserSettingsService(IDbContextFactory<ApplicationDbContext> contextFactory)
        : IUserSettingsService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;

        public async Task<UserSettings?> GetUserSettingsAsync(string userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            Console.WriteLine($"[UserSettingsService] Getting settings for userId: {userId}");
            var settings = await context
                .UserSettings.AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                Console.WriteLine($"[UserSettingsService] No settings found for userId: {userId}");
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

            Console.WriteLine($"[UserSettingsService] Settings retrieved.");
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
            Console.WriteLine($"[UserSettingsService] Saving settings for userId: {userId}.");
            var settings = await context.UserSettings.FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                // Check if user actually exists to avoid FK error
                var userExists = await context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    Console.WriteLine(
                        $"[UserSettingsService] User {userId} not found. Cannot save settings."
                    );
                    throw new InvalidOperationException("User not found.");
                }

                settings = new UserSettings { UserId = userId, CreatedDate = DateTime.UtcNow };
                context.UserSettings.Add(settings);
            }

            settings.OpenAIApiKey = !string.IsNullOrEmpty(openAiApiKey)
                ? Encrypt(openAiApiKey)
                : null;
            settings.GoogleGeminiApiKey = !string.IsNullOrEmpty(googleGeminiApiKey)
                ? Encrypt(googleGeminiApiKey)
                : null;
            settings.ClaudeApiKey = !string.IsNullOrEmpty(claudeApiKey)
                ? Encrypt(claudeApiKey)
                : null;
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
            Console.WriteLine("[UserSettingsService] Settings saved to database.");
        }

        private static string Encrypt(string clearText)
        {
            // Simple Base64 encoding for storing API keys (for demo purposes only)
            try
            {
                var bytes = Encoding.UTF8.GetBytes(clearText);
                var result = Convert.ToBase64String(bytes);
                Console.WriteLine(
                    $"[UserSettingsService] Encrypting '{clearText[..Math.Min(3, clearText.Length)]}...' -> '{result[..Math.Min(3, result.Length)]}...'"
                );
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Encryption failed: {ex}");
                return string.Empty;
            }
        }

        private static string Decrypt(string cipherText)
        {
            // Simple Base64 decoding for stored API keys
            try
            {
                var bytes = Convert.FromBase64String(cipherText);
                var result = Encoding.UTF8.GetString(bytes);
                Console.WriteLine(
                    $"[UserSettingsService] Decrypting '{cipherText[..Math.Min(3, cipherText.Length)]}...' -> '{result[..Math.Min(3, result.Length)]}...'"
                );
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption failed: {ex}");
                return string.Empty;
            }
        }
    }
}
