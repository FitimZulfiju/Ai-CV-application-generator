namespace AiCV.Application.Interfaces;

public interface IModelAvailabilityService
{
    Task<List<AIModel>> GetAvailableModelsAsync();
}
