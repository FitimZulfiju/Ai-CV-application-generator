namespace AiCV.Application.Interfaces
{
    public interface IUserSettingsService
    {
        Task<UserSettings?> GetUserSettingsAsync(string userId);
        Task SaveUserSettingsAsync(
            string userId,
            string? openAiApiKey,
            string? googleGeminiApiKey,
            string? claudeApiKey,
            string? groqApiKey,
            string? deepSeekApiKey,
            string? openRouterApiKey,
            AIProvider defaultProvider,
            string? defaultModelId
        );
    }
}
