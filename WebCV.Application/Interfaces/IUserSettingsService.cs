namespace WebCV.Application.Interfaces
{
    public interface IUserSettingsService
    {
        Task<UserSettings?> GetUserSettingsAsync(string userId);
        Task SaveUserSettingsAsync(string userId, string openAiApiKey, string googleGeminiApiKey, string defaultModel);
    }
}
