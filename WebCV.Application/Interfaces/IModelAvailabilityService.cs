namespace WebCV.Application.Interfaces;

public interface IModelAvailabilityService
{
    Task<List<AIModel>> GetAvailableModelsAsync();
    Task<bool> IsLocalAIAvailableAsync();
}
