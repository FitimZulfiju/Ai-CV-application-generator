namespace AiCV.Infrastructure.Services;

public class AIServiceFactory(
    IUserAIConfigurationService userAIConfigurationService,
    IHttpClientFactory httpClientFactory,
    IStringLocalizer<AicvResources> localizer
) : IAIServiceFactory
{
    private readonly IUserAIConfigurationService _userAIConfigurationService =
        userAIConfigurationService;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IStringLocalizer<AicvResources> _localizer = localizer;

    public async Task<IAIService> GetServiceAsync(
        AIProvider provider,
        string userId,
        string? modelId = null
    )
    {
        var configurations = await _userAIConfigurationService.GetConfigurationsAsync(userId);

        var config =
            configurations.FirstOrDefault(c => c.Provider == provider && c.IsActive)
            ?? configurations.FirstOrDefault(c => c.Provider == provider);

        if (config == null || string.IsNullOrEmpty(config.ApiKey))
        {
            throw new InvalidOperationException(
                $"{provider} is not configured. Please add a configuration in Settings."
            );
        }

        string? selectedModelId = modelId ?? config.ModelId;

        return provider switch
        {
            AIProvider.OpenAI => new OpenAIService(
                config.ApiKey,
                selectedModelId ?? "gpt-4o",
                _localizer
            ),
            AIProvider.GoogleGemini => new GoogleGeminiService(
                _httpClientFactory.CreateClient(),
                config.ApiKey,
                selectedModelId ?? "gemini-2.0-flash-exp",
                _localizer
            ),
            AIProvider.Claude => new ClaudeService(
                _httpClientFactory.CreateClient(),
                config.ApiKey,
                selectedModelId ?? "claude-3-5-haiku-20241022",
                _localizer
            ),
            AIProvider.Groq => new GroqService(
                _httpClientFactory.CreateClient(),
                config.ApiKey,
                selectedModelId ?? "llama-3.3-70b-versatile",
                _localizer
            ),
            AIProvider.DeepSeek => new DeepSeekService(
                _httpClientFactory.CreateClient(),
                config.ApiKey,
                selectedModelId ?? "deepseek-chat",
                _localizer
            ),
            AIProvider.OpenRouter => CreateOpenRouterService(
                config.ApiKey,
                _httpClientFactory,
                selectedModelId ?? "google/gemini-2.0-flash-exp:free",
                _localizer
            ),
            _ => throw new ArgumentException("Invalid AI Provider", nameof(provider)),
        };
    }

    private static OpenRouterService CreateOpenRouterService(
        string apiKey,
        IHttpClientFactory httpClientFactory,
        string modelId,
        IStringLocalizer<AicvResources> localizer
    )
    {
        var httpClient = httpClientFactory.CreateClient();
        return new OpenRouterService(httpClient, apiKey, modelId, localizer);
    }

    public static IAIService CreateService(
        AIProvider provider,
        string apiKey,
        string modelId,
        IStringLocalizer<AicvResources> localizer,
        HttpClient? httpClient = null
    )
    {
        httpClient ??= new HttpClient();

        return provider switch
        {
            AIProvider.OpenAI => new OpenAIService(apiKey, modelId, localizer),
            AIProvider.GoogleGemini => new GoogleGeminiService(
                httpClient,
                apiKey,
                modelId,
                localizer
            ),
            AIProvider.Claude => new ClaudeService(httpClient, apiKey, modelId, localizer),
            AIProvider.Groq => new GroqService(httpClient, apiKey, modelId, localizer),
            AIProvider.DeepSeek => new DeepSeekService(httpClient, apiKey, modelId, localizer),
            AIProvider.OpenRouter => new OpenRouterService(
                httpClient,
                apiKey,
                modelId,
                localizer
            ),
            _ => throw new ArgumentException("Invalid AI Provider", nameof(provider)),
        };
    }
}
