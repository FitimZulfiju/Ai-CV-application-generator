namespace AiCV.Application.Interfaces;

public interface IModelDiscoveryService
{
    Task<ModelDiscoveryResult> DiscoverModelsAsync(AIProvider provider, string apiKey);
    List<AIModelDto> GetFallbackModels(AIProvider provider);
}

public class AIModelDto
{
    public string ModelId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string CostType { get; set; } = "Paid";
    public List<string> Notes { get; set; } = [];
}

public class ModelDiscoveryResult
{
    public bool Success { get; set; }
    public List<AIModelDto> Models { get; set; } = [];
    public string? ErrorMessage { get; set; }
}
