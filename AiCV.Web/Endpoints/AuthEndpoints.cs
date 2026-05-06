namespace AiCV.Web.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        // POST /logout
        app.MapPost(
            $"/{NavUri.LogoutPage}",
            async (SignInManager<User> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return Results.Redirect($"/{NavUri.LoginPage}");
            }
        );

        // GET /logout-direct — for Blazor components (avoids "Headers are read-only" error)
        app.MapGet(
            "/logout-direct",
            async (SignInManager<User> signInManager, ILogger<Program> logger) =>
            {
                logger.LogInformation("Direct logout requested.");
                await signInManager.SignOutAsync();
                return Results.Redirect("/");
            }
        );

        // POST /perform-login
        app.MapPost(
                "/perform-login",
                async (
                    SignInManager<User> signInManager,
                    [FromForm] string email,
                    [FromForm] string password,
                    [FromForm] bool? rememberMe
                ) =>
                {
                    var result = await signInManager.PasswordSignInAsync(
                        email,
                        password,
                        rememberMe ?? false,
                        lockoutOnFailure: false
                    );
                    if (result.Succeeded)
                        return Results.Redirect("/");
                    if (result.IsLockedOut)
                        return Results.Redirect($"/{NavUri.LoginPage}?error=AccountLocked");
                    return Results.Redirect($"/{NavUri.LoginPage}?error=InvalidLoginAttempt");
                }
            )
            .DisableAntiforgery();

        // GET /perform-login — auto-login after registration
        app.MapGet(
            "/perform-login",
            async (
                SignInManager<User> signInManager,
                [FromQuery] string email,
                [FromQuery] string password
            ) =>
            {
                var result = await signInManager.PasswordSignInAsync(
                    email,
                    password,
                    isPersistent: false,
                    lockoutOnFailure: false
                );
                if (result.Succeeded)
                    return Results.Redirect("/");
                return Results.Redirect($"/{NavUri.LoginPage}?error=InvalidLoginAttempt");
            }
        );

        // GET /external-login/{provider} — challenge
        app.MapGet(
            "/external-login/{provider}",
            async (string provider, SignInManager<User> signInManager, HttpContext httpContext) =>
            {
                await signInManager.SignOutAsync();
                await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                const string redirectUrl = "/external-login-callback";
                var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                properties.Items["prompt"] = "select_account";

                return Results.Challenge(properties, [provider]);
            }
        );

        // GET /external-login-callback
        app.MapGet(
            "/external-login-callback",
            async (
                SignInManager<User> signInManager,
                UserManager<User> userManager,
                ILogger<Program> logger,
                HttpContext _,
                IServiceProvider serviceProvider
            ) =>
            {
                var info = await signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    logger.LogWarning(
                        "External login info is null. This usually means the correlation cookie was lost or expired."
                    );
                    return Results.Redirect($"/{NavUri.LoginPage}?error=ExternalLoginFailed");
                }

                var signInResult = await signInManager.ExternalLoginSignInAsync(
                    info.LoginProvider,
                    info.ProviderKey,
                    isPersistent: false,
                    bypassTwoFactor: true
                );

                if (signInResult.Succeeded)
                    return Results.Redirect("/");

                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                    return Results.Redirect($"/{NavUri.LoginPage}?error=ExternalEmailNotProvided");

                var user = await userManager.FindByEmailAsync(email)
                        ?? await userManager.FindByNameAsync(email);

                if (user == null)
                {
                    user = new User
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                    };

                    IdentityResult createResult;
                    try
                    {
                        createResult = await userManager.CreateAsync(user);
                    }
                    catch (Exception ex) when (
                        ex.ToString().Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                        ex.ToString().Contains("UniqueIndex", StringComparison.OrdinalIgnoreCase) ||
                        ex.ToString().Contains("UserNameIndex", StringComparison.OrdinalIgnoreCase))
                    {
                        user = await userManager.FindByEmailAsync(email)
                            ?? await userManager.FindByNameAsync(email);

                        if (user == null)
                            return Results.Redirect($"/{NavUri.LoginPage}?error=ExternalLoginFailed");

                        createResult = IdentityResult.Success;
                    }

                    if (!createResult.Succeeded)
                    {
                        logger.LogError(
                            "Failed to create user during external login for provider {Provider}. ErrorCodes: {ErrorCodes}",
                            info.LoginProvider,
                            string.Join(", ", createResult.Errors.Select(e => e.Code))
                        );

                        if (createResult.Errors.Any(e => e.Code == "DuplicateEmail" || e.Code == "DuplicateUserName"))
                        {
                            user = await userManager.FindByEmailAsync(email)
                                ?? await userManager.FindByNameAsync(email);

                            if (user == null)
                                return Results.Redirect($"/{NavUri.LoginPage}?error=AccountAlreadyExists");
                        }
                        else
                        {
                            return Results.Redirect($"/{NavUri.LoginPage}?error=RegistrationFailed");
                        }
                    }
                    else if (!createResult.Errors.Any())
                    {
                        await userManager.AddToRoleAsync(user, Roles.User);

                        using var scope = serviceProvider.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var profile = new CandidateProfile
                        {
                            UserId = user.Id,
                            FullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email.Split('@')[0],
                            Email = email,
                            Skills = [],
                            WorkExperience = [],
                            Educations = [],
                        };
                        dbContext.CandidateProfiles.Add(profile);
                        await dbContext.SaveChangesAsync();
                    }
                }

                var logins = await userManager.GetLoginsAsync(user);
                if (!logins.Any(l => l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey))
                {
                    var addLoginResult = await userManager.AddLoginAsync(user, info);
                    if (!addLoginResult.Succeeded)
                    {
                        logger.LogError(
                            "Failed to add external login for user from provider {Provider}. ErrorCodes: {ErrorCodes}",
                            info.LoginProvider,
                            string.Join(", ", addLoginResult.Errors.Select(e => e.Code))
                        );
                    }
                }

                await signInManager.SignInAsync(user, isPersistent: false);
                return Results.Redirect("/");
            }
        );

        return app;
    }
}
