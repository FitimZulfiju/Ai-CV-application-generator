namespace AiCV.Infrastructure.Services;

public class AIServiceFactory(
    IUserAIConfigurationService userAIConfigurationService,
    IHttpClientFactory httpClientFactory,
    IStringLocalizer<AicvResources> localizer,
    IConfiguration configuration,
    IMemoryCache cache
) : IAIServiceFactory
{
    private readonly IUserAIConfigurationService _userAIConfigurationService =
        userAIConfigurationService;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IStringLocalizer<AicvResources> _localizer = localizer;
    private readonly IConfiguration _configuration = configuration;
    private readonly IMemoryCache _cache = cache;

    private const string GoogleOAuthRefreshPrefix = "oauth_refresh:google:";

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

        // Google Gemini: OAuth flow — exchange stored refresh token for a fresh access token
        if (provider == AIProvider.GoogleGemini &&
            config.ApiKey?.StartsWith(GoogleOAuthRefreshPrefix) == true)
        {
            var refreshToken = config.ApiKey[GoogleOAuthRefreshPrefix.Length..];
            var accessToken = await RefreshGoogleAccessTokenAsync(userId, refreshToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException(
                    "Failed to refresh your Google account token. " +
                    "Please reconnect your Google account in Settings."
                );
            }

            return new GoogleGeminiService(
                _httpClientFactory.CreateClient(),
                accessToken,
                selectedModelId ?? "gemini-2.0-flash-exp",
                _localizer,
                isOAuthToken: true
            );
        }

        return provider switch
        {
            AIProvider.OpenAI => new OpenAIService(
                config.ApiKey ?? "",
                selectedModelId ?? "gpt-4o",
                _localizer
            ),
            AIProvider.GoogleGemini => new GoogleGeminiService(
                _httpClientFactory.CreateClient(),
                config.ApiKey ?? "",
                selectedModelId ?? "gemini-2.0-flash-exp",
                _localizer
            ),
            AIProvider.Claude => new ClaudeService(
                _httpClientFactory.CreateClient(),
                config.ApiKey ?? "",
                selectedModelId ?? "claude-3-5-haiku-20241022",
                _localizer
            ),
            AIProvider.Groq => new GroqService(
                _httpClientFactory.CreateClient(),
                config.ApiKey ?? "",
                selectedModelId ?? "llama-3.3-70b-versatile",
                _localizer
            ),
            AIProvider.DeepSeek => new DeepSeekService(
                _httpClientFactory.CreateClient(),
                config.ApiKey ?? "",
                selectedModelId ?? "deepseek-chat",
                _localizer
            ),
            AIProvider.OpenRouter => CreateOpenRouterService(
                config.ApiKey ?? "",
                _httpClientFactory,
                selectedModelId ?? "google/gemini-2.0-flash-exp:free",
                _localizer
            ),
            _ => throw new ArgumentException("Invalid AI Provider", nameof(provider)),
        };
    }

    /// <summary>
    /// Exchanges a stored Google OAuth refresh token for a short-lived access token.
    /// The result is cached for 50 minutes (tokens last 60 min).
    /// </summary>
    private async Task<string?> RefreshGoogleAccessTokenAsync(string userId, string refreshToken)
    {
        var cacheKey = $"gemini_access_token_{userId}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        var clientId = _configuration["Authentication:Google:ClientId"];
        var clientSecret = _configuration["Authentication:Google:ClientSecret"];
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return null;

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent([
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
            ])
        );

        if (!response.IsSuccessStatusCode)
            return null;

        var tokenData = await response.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenData.TryGetProperty("access_token", out var at) ? at.GetString() : null;
        var expiresIn = tokenData.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 3600;

        if (!string.IsNullOrEmpty(accessToken))
            _cache.Set(cacheKey, accessToken, TimeSpan.FromSeconds(expiresIn - 120));

        return accessToken;
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
