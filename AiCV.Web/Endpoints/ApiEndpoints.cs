namespace AiCV.Web.Endpoints;

public static class ApiEndpoints
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        // GET /api/version — polled by the client for update awareness
        app.MapGet(
                "/api/version",
                (IUpdateCheckService updateCheckService) =>
                {
                    var version = AppVersionProvider.GetDisplayVersion();
                    var isUpdateAvailable = updateCheckService.IsUpdateAvailable;
                    var newVersionTag = updateCheckService.NewVersionTag;
                    var isUpdateScheduled = updateCheckService.IsUpdateScheduled;
                    var scheduledUpdateTime = updateCheckService.ScheduledUpdateTime;
                    double? secondsRemaining = scheduledUpdateTime.HasValue
                        ? (scheduledUpdateTime.Value - DateTime.UtcNow).TotalSeconds
                        : null;

                    return Results.Ok(new
                    {
                        version,
                        isUpdateAvailable,
                        newVersionTag,
                        isUpdateScheduled,
                        scheduledUpdateTime,
                        secondsRemaining,
                    });
                }
            )
            .AllowAnonymous();

        // POST /api/schedule-update — triggers a 3-minute countdown on the server
        app.MapPost(
                "/api/schedule-update",
                (IUpdateCheckService updateCheckService, IWebHostEnvironment env, ILogger<Program> logger) =>
                {
                    if (env.IsDevelopment())
                        return Results.BadRequest("Scheduled updates are disabled in Development.");

                    logger.LogWarning("Manual update schedule requested via API.");
                    updateCheckService.ScheduleUpdate(180);

                    for (var attempt = 0;
                         updateCheckService.ScheduledUpdateTime == null && attempt < 5;
                         attempt++)
                    {
                        Thread.Sleep(50);
                    }

                    double? seconds = updateCheckService.ScheduledUpdateTime.HasValue
                        ? (updateCheckService.ScheduledUpdateTime.Value - DateTime.UtcNow).TotalSeconds
                        : null;

                    return Results.Ok(new
                    {
                        scheduledUpdateTime = updateCheckService.ScheduledUpdateTime,
                        secondsRemaining = seconds,
                    });
                }
            )
            .RequireAuthorization();

        // POST /api/trigger-update — immediate update trigger
        app.MapPost(
                "/api/trigger-update",
                async (IUpdateCheckService updateCheckService) =>
                {
                    var success = await updateCheckService.TriggerUpdateAsync();
                    return success ? Results.Ok() : Results.Problem("Failed to trigger update");
                }
            )
            .RequireAuthorization();

        // GET /culture/set — persists the user's culture preference as a cookie
        app.MapGet(
            "/culture/set",
            (string culture, string redirectUri, HttpContext httpContext) =>
            {
                if (culture != null)
                {
                    httpContext.Response.Cookies.Append(
                        CookieRequestCultureProvider.DefaultCookieName,
                        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                        new CookieOptions
                        {
                            Expires = DateTimeOffset.UtcNow.AddYears(1),
                            Secure = true,
                            HttpOnly = true,
                            SameSite = SameSiteMode.Lax,
                        }
                    );
                }
                return Results.Redirect(redirectUri);
            }
        );

        return app;
    }
}
