namespace AiCV.Application.Interfaces;

public interface IModelDiscoveryService
{
    Task<ModelDiscoveryResult> DiscoverModelsAsync(AIProvider provider, string apiKey);
    List<string> GetFallbackModels(AIProvider provider);
}

public class ModelDiscoveryResult
{
    public bool Success { get; set; }
    public List<string> Models { get; set; } = [];
    public string? ErrorMessage { get; set; }
}
