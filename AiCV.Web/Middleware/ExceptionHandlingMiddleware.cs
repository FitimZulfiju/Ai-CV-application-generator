namespace AiCV.Web.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
{
    private readonly RequestDelegate _next = next;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Microsoft.AspNetCore.Authentication.AuthenticationFailureException ex)
        {
            // Handle OAuth failures (like invalid_grant/expired token) gracefully
            using var scope = _scopeFactory.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<ISystemLogService>();

            await logService.LogWarningAsync(
                message: $"External login failure: {ex.Message}. Path: {context.Request.Path}",
                source: "ExceptionHandlingMiddleware"
            );

            // Redirect to login page with error
            context.Response.Redirect($"/{NavUri.LoginPage}?error=ExternalLoginFailed");
        }
        catch (Exception ex)
        {
            // Create a scope to resolve scoped services like ISystemLogService
            using var scope = _scopeFactory.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<ISystemLogService>();

            // Get current user if available
            var userId =
                context.User?.Identity?.IsAuthenticated == true
                    ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    : null;

            await logService.LogErrorAsync(
                message: ex.Message,
                stackTrace: ex.StackTrace,
                source: ex.Source,
                requestPath: context.Request.Path,
                userId: userId
            );

            // Re-throw to let existing error handling (UseExceptionHandler) take over
            throw;
        }
    }
}
