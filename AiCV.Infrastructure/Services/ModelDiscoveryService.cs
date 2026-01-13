namespace AiCV.Infrastructure.Services;

public class ModelDiscoveryService(
    IHttpClientFactory httpClientFactory,
    ILogger<ModelDiscoveryService> logger
) : IModelDiscoveryService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<ModelDiscoveryService> _logger = logger;

    public async Task<ModelDiscoveryResult> DiscoverModelsAsync(AIProvider provider, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new ModelDiscoveryResult
            {
                Success = false,
                ErrorMessage = "API key is required",
            };
        }

        try
        {
            return provider switch
            {
                AIProvider.OpenAI => await DiscoverOpenAIModelsAsync(apiKey),
                AIProvider.GoogleGemini => await DiscoverGeminiModelsAsync(apiKey),
                AIProvider.Groq => await DiscoverGroqModelsAsync(apiKey),
                AIProvider.DeepSeek => await DiscoverDeepSeekModelsAsync(apiKey),
                AIProvider.OpenRouter => await DiscoverOpenRouterModelsAsync(apiKey),
                AIProvider.Claude => new ModelDiscoveryResult
                {
                    Success = true,
                    Models = GetFallbackModels(AIProvider.Claude),
                },
                _ => new ModelDiscoveryResult
                {
                    Success = false,
                    ErrorMessage = "Provider does not support discovery",
                },
            };
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Error discovering models for {Provider}", provider);
            }
            return new ModelDiscoveryResult
            {
                Success = true,
                Models = GetFallbackModels(provider),
            };
        }
    }

    private async Task<ModelDiscoveryResult> DiscoverOpenAIModelsAsync(string apiKey)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            apiKey
        );
        var response = await client.GetAsync("https://api.openai.com/v1/models");
        if (!response.IsSuccessStatusCode)
        {
            return new ModelDiscoveryResult
            {
                Success = true,
                Models = GetFallbackModels(AIProvider.OpenAI),
            };
        }

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var models = json
            .RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(m => m.GetProperty("id").GetString()!)
            .Where(id => id.StartsWith("gpt-") || id.StartsWith("o1") || id.StartsWith("o3"))
            .OrderDescending()
            .ToList();

        return new ModelDiscoveryResult
        {
            Success = true,
            Models = models.Count > 0 ? models : GetFallbackModels(AIProvider.OpenAI),
        };
    }

    private async Task<ModelDiscoveryResult> DiscoverGeminiModelsAsync(string apiKey)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}"
        );
        if (!response.IsSuccessStatusCode)
        {
            return new ModelDiscoveryResult
            {
                Success = true,
                Models = GetFallbackModels(AIProvider.GoogleGemini),
            };
        }

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var models = json
            .RootElement.GetProperty("models")
            .EnumerateArray()
            .Select(m => m.GetProperty("name").GetString()!.Replace("models/", ""))
            .Where(id => id.StartsWith("gemini-"))
            .OrderDescending()
            .ToList();

        return new ModelDiscoveryResult
        {
            Success = true,
            Models = models.Count > 0 ? models : GetFallbackModels(AIProvider.GoogleGemini),
        };
    }

    private async Task<ModelDiscoveryResult> DiscoverGroqModelsAsync(string apiKey)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            apiKey
        );
        var response = await client.GetAsync("https://api.groq.com/openai/v1/models");
        if (!response.IsSuccessStatusCode)
        {
            return new ModelDiscoveryResult
            {
                Success = true,
                Models = GetFallbackModels(AIProvider.Groq),
            };
        }

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var models = json
            .RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(m => m.GetProperty("id").GetString()!)
            .OrderDescending()
            .ToList();

        return new ModelDiscoveryResult
        {
            Success = true,
            Models = models.Count > 0 ? models : GetFallbackModels(AIProvider.Groq),
        };
    }

    private async Task<ModelDiscoveryResult> DiscoverDeepSeekModelsAsync(string apiKey)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            apiKey
        );
        var response = await client.GetAsync("https://api.deepseek.com/v1/models");
        if (!response.IsSuccessStatusCode)
        {
            return new ModelDiscoveryResult
            {
                Success = true,
                Models = GetFallbackModels(AIProvider.DeepSeek),
            };
        }

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var models = json
            .RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(m => m.GetProperty("id").GetString()!)
            .OrderDescending()
            .ToList();

        return new ModelDiscoveryResult
        {
            Success = true,
            Models = models.Count > 0 ? models : GetFallbackModels(AIProvider.DeepSeek),
        };
    }

    private async Task<ModelDiscoveryResult> DiscoverOpenRouterModelsAsync(string apiKey)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            apiKey
        );
        client.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/FitimZulfiju/AiCV");
        client.DefaultRequestHeaders.Add("X-Title", "AiCV Application Generator");

        var response = await client.GetAsync("https://openrouter.ai/api/v1/models");
        if (!response.IsSuccessStatusCode)
        {
            return new ModelDiscoveryResult
            {
                Success = false,
                ErrorMessage = $"API returned {response.StatusCode}",
            };
        }

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var models = json
            .RootElement.GetProperty("data")
            .EnumerateArray()
            .Select(m => m.GetProperty("id").GetString()!)
            .Order()
            .ToList();

        return new ModelDiscoveryResult { Success = true, Models = models };
    }

    public List<string> GetFallbackModels(AIProvider provider) =>
        provider switch
        {
            AIProvider.OpenAI => ["gpt-4o", "gpt-4o-mini", "o1-preview", "o1-mini"],
            AIProvider.GoogleGemini =>
            [
                "gemini-2.0-flash-exp",
                "gemini-1.5-pro",
                "gemini-1.5-flash",
            ],
            AIProvider.Claude =>
            [
                "claude-3-5-sonnet-20241022",
                "claude-3-5-haiku-20241022",
                "claude-3-opus-20240229",
            ],
            AIProvider.Groq =>
            [
                "llama-3.3-70b-versatile",
                "llama-3.1-8b-instant",
                "mixtral-8x7b-32768",
            ],
            AIProvider.DeepSeek => ["deepseek-chat", "deepseek-reasoner"],
            _ => [],
        };
}
