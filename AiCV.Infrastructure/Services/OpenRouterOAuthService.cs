namespace AiCV.Infrastructure.Services;

public class OpenRouterOAuthService(
    IHttpClientFactory httpClientFactory,
    ILogger<OpenRouterOAuthService> logger) : IOpenRouterOAuthService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<OpenRouterOAuthService> _logger = logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Builds the OpenRouter auth redirect URL.
    /// PKCE (code_challenge) is intentionally omitted — OpenRouter rejects both
    /// S256 and plain methods at the exchange step. CSRF protection is provided
    /// by the random opaque <paramref name="state"/> parameter instead.
    /// </summary>
    public string BuildAuthRedirectUrl(string callbackUrl, string state)
    {
        return "https://openrouter.ai/auth" +
               $"?callback_url={Uri.EscapeDataString(callbackUrl)}" +
               "&site_name=MF-AiCV" +
               $"&state={state}";
    }

    /// <summary>
    /// Exchanges an OpenRouter authorization code for an API key.
    /// Returns (Key, null) on success or (null, errorBody) on failure.
    /// </summary>
    public async Task<(string? Key, string? Error)> ExchangeCodeForKeyAsync(string code)
    {
        var client = _httpClientFactory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "https://openrouter.ai/api/v1/auth/keys",
            new { code }
        );

        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("[OpenRouter] Exchange failed: {Status} - {Body}", (int)response.StatusCode, body);
            return (null, body);
        }

        var result = JsonSerializer.Deserialize<OpenRouterKeyResponse>(body, _jsonOptions);
        return (result?.Key, null);
    }
}

file class OpenRouterKeyResponse
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }
}
