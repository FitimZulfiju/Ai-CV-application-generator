using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebCV.Infrastructure.Services
{
    public class AIServiceFactory(IUserSettingsService userSettingsService, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IConfiguration configuration) : IAIServiceFactory
    {
        private readonly IUserSettingsService _userSettingsService = userSettingsService;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ILoggerFactory _loggerFactory = loggerFactory;
        private readonly IConfiguration _configuration = configuration;

        public async Task<IAIService> GetServiceAsync(AIProvider provider, string userId, AIModel? model = null)
        {
            var settings = await _userSettingsService.GetUserSettingsAsync(userId);
            
            // Use passed model if provided, otherwise fallback to settings default, then to system default
            var selectedModel = model ?? settings?.DefaultModel ?? Domain.AIModel.Phi3Mini;

            return provider switch
            {
                AIProvider.OpenAI => CreateOpenAIService(settings),
                AIProvider.GoogleGemini => CreateGoogleGeminiService(settings, _httpClientFactory),
                AIProvider.Local => new LocalAIService(selectedModel, _loggerFactory.CreateLogger<LocalAIService>(), _httpClientFactory.CreateClient("LocalAI"), _configuration["ConnectionStrings:Ollama"]),
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
