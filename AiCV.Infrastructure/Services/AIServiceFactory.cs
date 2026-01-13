namespace AiCV.Infrastructure.Services
{
    public class AIServiceFactory(
        IUserAIConfigurationService userAIConfigurationService,
        IHttpClientFactory httpClientFactory
    ) : IAIServiceFactory
    {
        private readonly IUserAIConfigurationService _userAIConfigurationService =
            userAIConfigurationService;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        public async Task<IAIService> GetServiceAsync(
            AIProvider provider,
            string userId,
            string? modelId = null
        )
        {
            var configurations = await _userAIConfigurationService.GetConfigurationsAsync(userId);

            // Priority 1: User asked for specific provider -> Find config for that provider.
            // Prefer Active config if it matches the provider, otherwise take any config for that provider.
            var config =
                configurations.FirstOrDefault(c => c.Provider == provider && c.IsActive)
                ?? configurations.FirstOrDefault(c => c.Provider == provider);

            if (config == null || string.IsNullOrEmpty(config.ApiKey))
            {
                throw new InvalidOperationException(
                    $"{provider} is not configured. Please add a configuration in Settings."
                );
            }

            // Use modelId if passed, otherwise fallback to config's model, otherwise default string
            string? selectedModelId = modelId ?? config.ModelId;

            return provider switch
            {
                AIProvider.OpenAI => new OpenAIService(config.ApiKey, selectedModelId ?? "gpt-4o"),
                AIProvider.GoogleGemini => new GoogleGeminiService(
                    _httpClientFactory.CreateClient(),
                    config.ApiKey,
                    selectedModelId ?? "gemini-2.0-flash-exp"
                ),
                AIProvider.Claude => new ClaudeService(
                    _httpClientFactory.CreateClient(),
                    config.ApiKey,
                    selectedModelId ?? "claude-3-5-haiku-20241022"
                ),
                AIProvider.Groq => new GroqService(
                    _httpClientFactory.CreateClient(),
                    config.ApiKey,
                    selectedModelId ?? "llama-3.3-70b-versatile"
                ),
                AIProvider.DeepSeek => new DeepSeekService(
                    _httpClientFactory.CreateClient(),
                    config.ApiKey,
                    selectedModelId ?? "deepseek-chat"
                ),
                AIProvider.OpenRouter => CreateOpenRouterService(
                    config.ApiKey,
                    _httpClientFactory,
                    selectedModelId ?? "google/gemini-2.0-flash-exp:free"
                ),
                _ => throw new ArgumentException("Invalid AI Provider", nameof(provider)),
            };
        }

        private static OpenRouterService CreateOpenRouterService(
            string apiKey,
            IHttpClientFactory httpClientFactory,
            string modelId
        )
        {
            var httpClient = httpClientFactory.CreateClient();
            return new OpenRouterService(httpClient, apiKey, modelId);
        }
    }
}
