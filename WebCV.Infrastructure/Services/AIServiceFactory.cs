namespace WebCV.Infrastructure.Services
{
    public class AIServiceFactory(IUserSettingsService userSettingsService, IHttpClientFactory httpClientFactory) : IAIServiceFactory
    {
        private readonly IUserSettingsService _userSettingsService = userSettingsService;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        public async Task<IAIService> GetServiceAsync(AIProvider provider, string userId, AIModel? model = null)
        {
            var settings = await _userSettingsService.GetUserSettingsAsync(userId);

            // Use passed model if provided, otherwise fallback to settings default, then to system default
            AIModel selectedModel = model ?? settings?.DefaultModel ?? AIModel.Gpt4o;

            return provider switch
            {
                AIProvider.OpenAI => CreateOpenAIService(settings),
                AIProvider.GoogleGemini => CreateGoogleGeminiService(settings, _httpClientFactory),
                AIProvider.Anthropic => CreateClaudeService(settings, _httpClientFactory),
                AIProvider.Groq => CreateGroqService(settings, _httpClientFactory),
                AIProvider.DeepSeek => CreateDeepSeekService(settings, _httpClientFactory),
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

        private static ClaudeService CreateClaudeService(UserSettings? settings, IHttpClientFactory httpClientFactory)
        {
            var apiKey = settings?.ClaudeApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Claude API Key is not configured. Please go to Settings to configure it.");
            }

            var httpClient = httpClientFactory.CreateClient();
            return new ClaudeService(httpClient, apiKey);
        }

        private static GroqService CreateGroqService(UserSettings? settings, IHttpClientFactory httpClientFactory)
        {
            var apiKey = settings?.GroqApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Groq API Key is not configured. Please go to Settings to configure it.");
            }

            var httpClient = httpClientFactory.CreateClient();
            return new GroqService(httpClient, apiKey);
        }

        private static DeepSeekService CreateDeepSeekService(UserSettings? settings, IHttpClientFactory httpClientFactory)
        {
            var apiKey = settings?.DeepSeekApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("DeepSeek API Key is not configured. Please go to Settings to configure it.");
            }

            var httpClient = httpClientFactory.CreateClient();
            return new DeepSeekService(httpClient, apiKey);
        }
    }
}
