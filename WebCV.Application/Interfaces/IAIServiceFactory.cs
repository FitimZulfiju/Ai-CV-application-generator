namespace WebCV.Application.Interfaces
{
    public interface IAIServiceFactory
    {
        Task<IAIService> GetServiceAsync(AIProvider provider, string userId);
    }
}
