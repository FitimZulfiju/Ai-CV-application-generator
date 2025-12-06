namespace WebCV.Infrastructure.Services
{
    public class UserSettingsService(IDbContextFactory<ApplicationDbContext> contextFactory) : IUserSettingsService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;

        public async Task<UserSettings?> GetUserSettingsAsync(string userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            Console.WriteLine($"[UserSettingsService] Getting settings for userId: {userId}");
            var settings = await context.UserSettings
                .AsNoTracking()
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

            Console.WriteLine($"[UserSettingsService] Settings retrieved. Gemini Key Present: {!string.IsNullOrEmpty(settings.GoogleGeminiApiKey)}");
            return settings;
        }

        public async Task SaveUserSettingsAsync(string userId, string openAiApiKey, string googleGeminiApiKey, AIModel defaultModel)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            Console.WriteLine($"[UserSettingsService] Saving settings for userId: {userId}. Gemini Key Input Length: {googleGeminiApiKey?.Length ?? 0}");
            var settings = await context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (settings == null)
            {
                // Check if user actually exists to avoid FK error
                var userExists = await context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    Console.WriteLine($"[UserSettingsService] User {userId} not found. Cannot save settings.");
                    throw new InvalidOperationException("User not found.");
                }

                settings = new UserSettings
                {
                    UserId = userId,
                    CreatedDate = DateTime.UtcNow
                };
                context.UserSettings.Add(settings);
            }

            if (!string.IsNullOrEmpty(openAiApiKey))
            {
                settings.OpenAIApiKey = Encrypt(openAiApiKey);
            }
            else
            {
                settings.OpenAIApiKey = null;
            }

            if (!string.IsNullOrEmpty(googleGeminiApiKey))
            {
                var encrypted = Encrypt(googleGeminiApiKey);
                Console.WriteLine($"[UserSettingsService] Encrypted Gemini Key Length: {encrypted.Length}");
                settings.GoogleGeminiApiKey = encrypted;
            }
            else
            {
                Console.WriteLine("[UserSettingsService] Gemini Key is empty/null, setting to null.");
                settings.GoogleGeminiApiKey = null;
            }

            settings.DefaultModel = defaultModel;
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
                Console.WriteLine($"[UserSettingsService] Encrypting '{clearText[..Math.Min(3, clearText.Length)]}...' -> '{result[..Math.Min(3, result.Length)]}...'");
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
                Console.WriteLine($"[UserSettingsService] Decrypting '{cipherText[..Math.Min(3, cipherText.Length)]}...' -> '{result[..Math.Min(3, result.Length)]}...'");
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
