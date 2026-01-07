namespace AiCV.Infrastructure.Services;

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
        IConfiguration configuration
    )
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _ollamaEndpoint =
            configuration.GetConnectionString("Ollama") ?? "http://localhost:11434/api/tags";
        // Fix endpoint - should be /api/tags not /api/generate for listing models
        if (_ollamaEndpoint.EndsWith("/api/generate"))
        {
            _ollamaEndpoint = _ollamaEndpoint.Replace("/api/generate", "/api/tags");
        }
    }

    public Task<List<AIModel>> GetAvailableModelsAsync()
    {
        // Check cache first
        if (_cachedModels != null && DateTime.UtcNow < _cacheExpiry)
        {
            return Task.FromResult(_cachedModels);
        }

        var models = new List<AIModel>
        {
            // All cloud models are always available
            AIModel.Gpt4o,
            AIModel.Gemini20Flash,
            AIModel.Claude35Haiku,
            AIModel.Llama3370B,
            AIModel.DeepSeekV3,
        };

        // Cache the result
        _cachedModels = models;
        _cacheExpiry = DateTime.UtcNow.Add(_cacheTime);

        return Task.FromResult(models);
    }
}
