namespace AiCV.Application.Interfaces;

using AiCV.Domain;

public interface IUserSmtpSettingsService
{
    Task<UserSmtpSettings?> GetForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserSmtpSettings> UpdateAsync(string userId, UserSmtpSettings settings, CancellationToken cancellationToken = default);
}

