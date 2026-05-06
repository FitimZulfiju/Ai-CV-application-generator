namespace AiCV.Web.Endpoints;

public static class OAuthEndpoints
{
    public static WebApplication MapOAuthEndpoints(this WebApplication app)
    {
        // ─── OpenRouter OAuth: Start ─────────────────────────────────────────
        app.MapGet(
            "/connect/openrouter",
            (HttpContext httpContext, IMemoryCache cache, IOpenRouterOAuthService oauthService) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Results.Redirect($"/{NavUri.LoginPage}");

                var state = Guid.NewGuid().ToString("N");

                // Store userId keyed by state (10 min TTL) — state acts as CSRF token
                cache.Set($"or_state_{state}", userId, TimeSpan.FromMinutes(10));

                var callbackUrl =
                    $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/connect/openrouter/callback";

                // Allow overriding the base URL for local testing with ngrok/tunnel
                var appBaseUrl = httpContext.RequestServices
                    .GetRequiredService<IConfiguration>()["APP_BASE_URL"];
                if (!string.IsNullOrEmpty(appBaseUrl))
                    callbackUrl = $"{appBaseUrl.TrimEnd('/')}/connect/openrouter/callback";

                var redirectUrl = oauthService.BuildAuthRedirectUrl(callbackUrl, state);
                return Results.Redirect(redirectUrl);
            }
        ).RequireAuthorization();

        // ─── OpenRouter OAuth: Callback ──────────────────────────────────────
        app.MapGet(
            "/connect/openrouter/callback",
            async (
                [FromQuery] string? code,
                [FromQuery] string? state,
                [FromQuery] string? error,
                IMemoryCache cache,
                IOpenRouterOAuthService oauthService,
                IServiceProvider serviceProvider
            ) =>
            {
                if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                    return Results.Redirect($"/{NavUri.SettingsPage}?error=openrouter_cancelled");

                if (!cache.TryGetValue($"or_state_{state}", out string? userId) || string.IsNullOrEmpty(userId))
                    return Results.Redirect($"/{NavUri.SettingsPage}?error=openrouter_expired");

                cache.Remove($"or_state_{state}");

                var (apiKey, exchangeError) = await oauthService.ExchangeCodeForKeyAsync(code);
                if (string.IsNullOrEmpty(apiKey))
                {
                    var detail = Uri.EscapeDataString(exchangeError ?? "Unknown error");
                    return Results.Redirect($"/{NavUri.SettingsPage}?error=openrouter_exchange_failed&detail={detail}");
                }

                using var scope = serviceProvider.CreateScope();
                var configService = scope.ServiceProvider.GetRequiredService<IUserAIConfigurationService>();

                var configs = await configService.GetConfigurationsAsync(userId);
                var existing = configs.FirstOrDefault(c =>
                    c.Provider == AIProvider.OpenRouter && c.Name == "OpenRouter Account");

                var config = existing ?? new UserAIConfiguration
                {
                    UserId = userId,
                    Provider = AIProvider.OpenRouter,
                    Name = "OpenRouter Account",
                    ModelId = "google/gemini-2.0-flash-exp:free",
                    CostType = "Free",
                    Notes = "Connected via OpenRouter OAuth",
                };

                config.ApiKey = apiKey;
                await configService.SaveConfigurationAsync(config);

                return Results.Redirect($"/{NavUri.SettingsPage}?connected=openrouter");
            }
        ).AllowAnonymous();

        // ─── Google Gemini OAuth: Start ──────────────────────────────────────
        app.MapGet(
            "/connect/gemini",
            (HttpContext httpContext, IMemoryCache cache, IConfiguration configuration) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Results.Redirect($"/{NavUri.LoginPage}");

                var clientId = configuration["Authentication:Google:ClientId"];
                if (string.IsNullOrEmpty(clientId))
                    return Results.Redirect($"/{NavUri.SettingsPage}?error=google_not_configured");

                var state = Guid.NewGuid().ToString("N");
                cache.Set($"gemini_state_{state}", userId, TimeSpan.FromMinutes(10));

                var redirectUri =
                    $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/connect/gemini/callback";

                var authUrl =
                    "https://accounts.google.com/o/oauth2/v2/auth" +
                    $"?client_id={Uri.EscapeDataString(clientId)}" +
                    $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                    "&response_type=code" +
                    $"&scope={Uri.EscapeDataString("email https://www.googleapis.com/auth/generative-language")}" +
                    "&access_type=offline" +
                    "&prompt=select_account%20consent" +
                    $"&state={state}";

                return Results.Redirect(authUrl);
            }
        ).RequireAuthorization();

        // ─── Google Gemini OAuth: Callback ───────────────────────────────────
        app.MapGet(
            "/connect/gemini/callback",
            async (
                [FromQuery] string? code,
                [FromQuery] string? state,
                [FromQuery] string? error,
                HttpContext httpContext,
                IMemoryCache cache,
                IConfiguration configuration,
                IServiceProvider serviceProvider
            ) =>
            {
                if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                    return Results.Redirect($"/{NavUri.SettingsPage}?error=gemini_cancelled");

                if (!cache.TryGetValue($"gemini_state_{state}", out string? userId) || string.IsNullOrEmpty(userId))
                    return Results.Redirect($"/{NavUri.SettingsPage}?error=gemini_expired");

                cache.Remove($"gemini_state_{state}");

                var clientId = configuration["Authentication:Google:ClientId"]!;
                var clientSecret = configuration["Authentication:Google:ClientSecret"]!;
                var redirectUri =
                    $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/connect/gemini/callback";

                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var client = httpClientFactory.CreateClient();
                var tokenResponse = await client.PostAsync(
                    "https://oauth2.googleapis.com/token",
                    new FormUrlEncodedContent([
                        new KeyValuePair<string, string>("code", code),
                        new KeyValuePair<string, string>("client_id", clientId),
                        new KeyValuePair<string, string>("client_secret", clientSecret),
                        new KeyValuePair<string, string>("redirect_uri", redirectUri),
                        new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    ])
                );

                if (!tokenResponse.IsSuccessStatusCode)
                    return Results.Redirect($"/{NavUri.SettingsPage}?error=gemini_exchange_failed");

                var tokenData = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
                var refreshToken = tokenData.TryGetProperty("refresh_token", out var rt)
                    ? rt.GetString() : null;

                if (string.IsNullOrEmpty(refreshToken))
                    return Results.Redirect($"/{NavUri.SettingsPage}?error=gemini_no_refresh_token");

                using var scope = serviceProvider.CreateScope();
                var configService = scope.ServiceProvider.GetRequiredService<IUserAIConfigurationService>();
                var configs = await configService.GetConfigurationsAsync(userId);
                var existing = configs.FirstOrDefault(c =>
                    c.Provider == AIProvider.GoogleGemini &&
                    c.ApiKey?.StartsWith("oauth_refresh:google:") == true);

                var geminiConfig = existing ?? new UserAIConfiguration
                {
                    UserId = userId,
                    Provider = AIProvider.GoogleGemini,
                    Name = "Google Account",
                    ModelId = "gemini-2.0-flash-exp",
                    CostType = "Paid",
                    Notes = "Connected via Google OAuth",
                };

                geminiConfig.ApiKey = $"oauth_refresh:google:{refreshToken}";
                await configService.SaveConfigurationAsync(geminiConfig);

                return Results.Redirect($"/{NavUri.SettingsPage}?connected=gemini");
            }
        ).AllowAnonymous();

        return app;
    }
}
