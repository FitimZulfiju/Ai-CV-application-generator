namespace WebCV.Infrastructure.Services;

public class ModelAvailabilityService : IModelAvailabilityService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ModelAvailabilityService> _logger;
    private readonly string _ollamaEndpoint;

    // Cache to avoid hitting Ollama API repeatedly
    private List<AIModel>? _cachedModels;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private readonly TimeSpan _cacheTime = TimeSpan.FromMinutes(5);

    public ModelAvailabilityService(
        IHttpClientFactory httpClientFactory,
        ILogger<ModelAvailabilityService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _ollamaEndpoint = configuration.GetConnectionString("Ollama") ?? "http://localhost:11434/api/tags";
        // Fix endpoint - should be /api/tags not /api/generate for listing models
        if (_ollamaEndpoint.EndsWith("/api/generate"))
        {
            _ollamaEndpoint = _ollamaEndpoint.Replace("/api/generate", "/api/tags");
        }
    }

    public async Task<List<AIModel>> GetAvailableModelsAsync()
    {
        // Check cache first
        if (_cachedModels != null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedModels;
        }

        var models = new List<AIModel>
        {
            // Cloud models are always available
            AIModel.Gpt4o,
            AIModel.Gemini20Flash
        };

        // Check if local AI is available
        if (await IsLocalAIAvailableAsync())
        {
            var localModels = await GetInstalledLocalModelsAsync();
            models.AddRange(localModels);
        }

        // Cache the result
        _cachedModels = models;
        _cacheExpiry = DateTime.UtcNow.Add(_cacheTime);

        return models;
    }

    public async Task<bool> IsLocalAIAvailableAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("LocalAI");
            var response = await client.GetAsync(_ollamaEndpoint);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Local AI not available");
            return false;
        }
    }

    private async Task<List<AIModel>> GetInstalledLocalModelsAsync()
    {
        var installedModels = new List<AIModel>();

        try
        {
            var client = _httpClientFactory.CreateClient("LocalAI");
            var response = await client.GetAsync(_ollamaEndpoint);

            if (!response.IsSuccessStatusCode)
            {
                return installedModels;
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);

            if (!json.RootElement.TryGetProperty("models", out var modelsArray))
            {
                return installedModels;
            }

            var modelNames = new HashSet<string>();
            foreach (var model in modelsArray.EnumerateArray())
            {
                if (model.TryGetProperty("name", out var nameElement))
                {
                    var name = nameElement.GetString()?.ToLower() ?? "";
                    // Extract base name without tag (e.g., "phi3:latest" -> "phi3")
                    var baseName = name.Split(':')[0];
                    modelNames.Add(baseName);
                }
            }

            // Map installed models to AIModel enum
            if (modelNames.Contains("mistral"))
            {
                installedModels.Add(AIModel.Mistral7B);
            }
            if (modelNames.Contains("llama3.1"))
            {
                installedModels.Add(AIModel.Llama31_8B);
            }
            if (modelNames.Contains("phi3"))
            {
                installedModels.Add(AIModel.Phi3Mini);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check installed Ollama models");
        }

        return installedModels;
    }
}
