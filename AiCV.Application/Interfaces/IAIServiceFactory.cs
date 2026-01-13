namespace AiCV.Application.Interfaces
{
    public interface IAIServiceFactory
    {
        Task<IAIService> GetServiceAsync(
            AIProvider provider,
            string userId,
            string? modelId = null
        );
    }
}
