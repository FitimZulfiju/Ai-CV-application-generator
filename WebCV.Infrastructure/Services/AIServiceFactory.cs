namespace WebCV.Infrastructure.Services
{
    public class AIServiceFactory(IUserSettingsService userSettingsService, IHttpClientFactory httpClientFactory) : IAIServiceFactory
    {
        private readonly IUserSettingsService _userSettingsService = userSettingsService;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        public async Task<IAIService> GetServiceAsync(AIProvider provider, string userId)
        {
            var settings = await _userSettingsService.GetUserSettingsAsync(userId);

            return provider switch
            {
                AIProvider.OpenAI => CreateOpenAIService(settings),
                AIProvider.GoogleGemini => CreateGoogleGeminiService(settings, _httpClientFactory),
                _ => throw new ArgumentException("Invalid AI Provider", nameof(provider))
            };
        }

        private static OpenAIService CreateOpenAIService(UserSettings? settings)
        {
            var apiKey = settings?.OpenAIApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenAI API Key is not configured. Please go to Settings to configure it.");
            }
            return new OpenAIService(apiKey);
        }

        private static GoogleGeminiService CreateGoogleGeminiService(UserSettings? settings, IHttpClientFactory httpClientFactory)
        {
            var apiKey = settings?.GoogleGeminiApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Google Gemini API Key is not configured. Please go to Settings to configure it.");
            }

            var httpClient = httpClientFactory.CreateClient();
            return new GoogleGeminiService(httpClient, apiKey);
        }
    }
}
