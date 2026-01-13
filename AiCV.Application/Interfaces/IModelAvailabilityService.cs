namespace AiCV.Application.Interfaces;

public interface IModelAvailabilityService
{
    Task<List<string>> GetAvailableModelsAsync();
}
