namespace AiCV.Application.Interfaces;

public interface IAutomationSettingsService
{
    Task<AutomationSettings?> GetForUserAsync(string userId);
    Task<AutomationSettings?> GetOrCreateAsync(string userId);
    Task<AutomationSettings> UpdateAsync(AutomationSettings settings, string userId);
    Task<bool> DeleteAsync(string userId);
}
