namespace AiCV.Infrastructure.Services;

public static class AIErrorMapper
{
    public static string MapError(
        AIProvider provider,
        string responseContent,
        HttpStatusCode statusCode,
        Microsoft.Extensions.Localization.IStringLocalizer localizer
    )
    {
        string? providerMessage = TryParseErrorMessage(responseContent);

        var baseMessage = statusCode switch
        {
            HttpStatusCode.Unauthorized => GetUnauthorizedMessage(provider, localizer),
            HttpStatusCode.PaymentRequired => GetPaymentRequiredMessage(provider, localizer),
            HttpStatusCode.Forbidden => GetForbiddenMessage(provider, localizer),
            HttpStatusCode.TooManyRequests => GetRateLimitMessage(provider, localizer),
            HttpStatusCode.BadRequest => localizer["InvalidRequest"].Value,
            HttpStatusCode.InternalServerError => localizer["RemoteServerError"].Value,
            _ => null,
        };

        if (baseMessage != null)
        {
            return providerMessage != null ? $"{baseMessage} ({providerMessage})" : baseMessage;
        }

        return $"{provider} Error: {statusCode} - {providerMessage ?? responseContent}";
    }

    private static string? TryParseErrorMessage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var error))
            {
                if (
                    error.ValueKind == JsonValueKind.Object
                    && error.TryGetProperty("message", out var msg)
                )
                {
                    return msg.GetString();
                }
                if (error.ValueKind == JsonValueKind.String)
                {
                    return error.GetString();
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string GetUnauthorizedMessage(
        AIProvider provider,
        IStringLocalizer localizer
    ) => string.Format(localizer["InvalidApiKey"], provider);

    private static string GetPaymentRequiredMessage(
        AIProvider provider,
        IStringLocalizer localizer
    ) => string.Format(localizer["InsufficientBalance"], provider);

    private static string GetForbiddenMessage(
        AIProvider provider,
        IStringLocalizer localizer
    ) => string.Format(localizer["ModelAccessRestricted"], provider);

    private static string GetRateLimitMessage(
        AIProvider provider,
        IStringLocalizer localizer
    ) => string.Format(localizer["RateLimitReached"], provider);
}
