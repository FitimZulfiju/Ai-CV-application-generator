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
            .Distinct()
            .Select(id => new AIModelDto
            {
                ModelId = id,
                Name = id,
                CostType = "Paid",
                Notes = ["Requires OpenAI credits/balance"],
            })
            .OrderByDescending(m => m.ModelId)
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
            .Select(id => new AIModelDto
            {
                ModelId = id,
                Name = id,
                CostType = "Paid",
                Notes = ["Free tier available with rate limits"],
            })
            .OrderByDescending(m => m.ModelId)
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
            .Select(id => new AIModelDto
            {
                ModelId = id,
                Name = id,
                CostType = "No per-token cost",
                Notes = ["Aggressive rate limits based on tier"],
            })
            .OrderByDescending(m => m.ModelId)
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
            .Select(id => new AIModelDto
            {
                ModelId = id,
                Name = id,
                CostType = "Paid",
                Notes = ["Requires DeepSeek API balance"],
            })
            .OrderByDescending(m => m.ModelId)
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
        var models = new List<AIModelDto>();

        foreach (var m in json.RootElement.GetProperty("data").EnumerateArray())
        {
            var id = m.GetProperty("id").GetString()!;
            var name = m.TryGetProperty("name", out var n) ? n.GetString() : id;
            var pricing = m.TryGetProperty("pricing", out var p) ? p : (JsonElement?)null;

            var costType = "Paid";
            if (pricing.HasValue)
            {
                var prompt = pricing.Value.TryGetProperty("prompt", out var pr)
                    ? pr.GetString()
                    : "0";
                var completion = pricing.Value.TryGetProperty("completion", out var cp)
                    ? cp.GetString()
                    : "0";

                if (
                    (prompt == "0" || prompt == "0.0") && (completion == "0" || completion == "0.0")
                )
                {
                    costType = "No per-token cost";
                }
            }

            var notes = new List<string>();
            if (costType == "No per-token cost")
            {
                notes.Add("Rate-limited");
                notes.Add("Availability not guaranteed");
            }
            else
            {
                notes.Add("Requires OpenRouter balance");
                notes.Add("May not be accessible with free-tier keys");
            }

            models.Add(
                new AIModelDto
                {
                    ModelId = id,
                    Name = name,
                    CostType = costType,
                    Notes = notes,
                }
            );
        }

        return new ModelDiscoveryResult
        {
            Success = true,
            Models = [.. models.OrderBy(m => m.Name)],
        };
    }

    public List<AIModelDto> GetFallbackModels(AIProvider provider)
    {
        var (ids, costType, notes) = provider switch
        {
            AIProvider.OpenAI => (
                ["gpt-4o", "gpt-4o-mini", "o1-preview", "o1-mini"],
                "Paid",
                ["Requires OpenAI credits/balance"]
            ),
            AIProvider.GoogleGemini => (
                ["gemini-2.0-flash-exp", "gemini-1.5-pro", "gemini-1.5-flash"],
                "Paid",
                ["Free tier available with rate limits"]
            ),
            AIProvider.Claude => (
                [
                    "claude-3-5-sonnet-20241022",
                    "claude-3-5-haiku-20241022",
                    "claude-3-opus-20240229",
                ],
                "Paid",
                ["Requires Anthropic credits"]
            ),
            AIProvider.Groq => (
                ["llama-3.3-70b-versatile", "llama-3.1-8b-instant", "mixtral-8x7b-32768"],
                "No per-token cost",
                ["Aggressive rate limits based on tier"]
            ),
            AIProvider.DeepSeek => (
                ["deepseek-chat", "deepseek-reasoner"],
                "Paid",
                ["Requires DeepSeek API balance"]
            ),
            _ => (Array.Empty<string>(), "Paid", new List<string>()),
        };

        return [.. ids.Select(id => new AIModelDto
            {
                ModelId = id,
                Name = id,
                CostType = costType,
                Notes = notes,
            })];
    }
}
