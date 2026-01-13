namespace AiCV.Application.Interfaces;

public interface IUserAIConfigurationService
{
    Task<List<UserAIConfiguration>> GetConfigurationsAsync(string userId);
    Task<UserAIConfiguration?> GetConfigurationAsync(int id, string userId);
    Task<UserAIConfiguration> SaveConfigurationAsync(UserAIConfiguration config);
    Task<bool> DeleteConfigurationAsync(int id, string userId);
    Task<UserAIConfiguration?> ActivateConfigurationAsync(int id, string userId);
    Task<UserAIConfiguration?> GetActiveConfigurationAsync(string userId);
}
