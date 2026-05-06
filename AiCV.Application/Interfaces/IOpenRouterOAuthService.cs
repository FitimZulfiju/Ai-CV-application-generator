namespace AiCV.Application.Interfaces;

public interface IOpenRouterOAuthService
{
    string BuildAuthRedirectUrl(string callbackUrl, string state);
    Task<(string? Key, string? Error)> ExchangeCodeForKeyAsync(string code);
}
