var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder
    .Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = true)
    .AddHubOptions(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
        options.HandshakeTimeout = TimeSpan.FromMinutes(2);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    });

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddMudServices();

// Add authorization
builder
    .Services.AddAuthorizationBuilder()
    // Add authorization
    .SetFallbackPolicy(null);

// Database

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=AiCV_db;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
);
builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(connectionString),
    ServiceLifetime.Scoped,
    ServiceLifetime.Singleton
);

// Add Identity
builder
    .Services.AddIdentity<User, IdentityRole>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;

        // User settings
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false; // Changed to false to allow login without email confirmation
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add External Authentication Providers
var authBuilder = builder.Services.AddAuthentication();

// Google Authentication
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

// Microsoft Authentication
var microsoftClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
var microsoftClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
if (!string.IsNullOrEmpty(microsoftClientId) && !string.IsNullOrEmpty(microsoftClientSecret))
{
    authBuilder.AddMicrosoftAccount(options =>
    {
        options.ClientId = microsoftClientId;
        options.ClientSecret = microsoftClientSecret;
        // Use /consumers/ endpoint for personal Microsoft accounts only
        options.AuthorizationEndpoint =
            "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize";
        options.TokenEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
    });
}

// GitHub Authentication
var githubClientId = builder.Configuration["Authentication:GitHub:ClientId"];
var githubClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
if (!string.IsNullOrEmpty(githubClientId) && !string.IsNullOrEmpty(githubClientSecret))
{
    authBuilder.AddGitHub(options =>
    {
        options.ClientId = githubClientId;
        options.ClientSecret = githubClientSecret;
        options.Scope.Add("user:email");
    });
}

// Configure cookie authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/{NavUri.LoginPage}";
    options.LogoutPath = $"/{NavUri.LogoutPage}";
    options.AccessDeniedPath = $"/{NavUri.LoginPage}";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Register Application Services
builder.Services.AddScoped<IPdfService, PdfService>();
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
QuestPDF.Settings.EnableDebugging = true;

// Register Lato Fonts
var fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LatoFont");
if (Directory.Exists(fontPath))
{
    foreach (var file in Directory.GetFiles(fontPath, "*.ttf"))
    {
        QuestPDF.Drawing.FontManager.RegisterFont(File.OpenRead(file));
    }
}

builder.Services.AddScoped<ICVService, CVService>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IJobPostScraper, JobPostScraper>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
builder.Services.AddScoped<IUserAIConfigurationService, UserAIConfigurationService>();

// Configure Forwarded Headers for Docker/Proxy scenarios
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
        | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddScoped<IAIServiceFactory, AIServiceFactory>();
builder.Services.AddScoped<IClipboardService, ClipboardService>();
builder.Services.AddScoped<IJobApplicationOrchestrator, JobApplicationOrchestrator>();
builder.Services.AddScoped<IModelAvailabilityService, ModelAvailabilityService>();
builder.Services.AddScoped<ILoadingService, LoadingService>();
builder.Services.AddScoped<IAdminStatisticsService, AdminStatisticsService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();
builder.Services.AddScoped<IModelDiscoveryService, ModelDiscoveryService>();
builder.Services.AddScoped<ClientPersistenceService>();

// Data Protection - Persist keys to file system to prevent API key loss on app restart
var keysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
if (!Directory.Exists(keysPath))
{
    Directory.CreateDirectory(keysPath);
}
builder
    .Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("AiCV.Web.v2");

builder.Services.AddLocalization();

// Configure supported cultures for localization
var supportedCultures = new[] { "en", "sq", "da" };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options
        .SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

// Update Check Service (Handles automatic detection and scheduling of container updates)
builder.Services.AddSingleton<UpdateCheckService>();
builder.Services.AddSingleton<IUpdateCheckService>(sp =>
    sp.GetRequiredService<UpdateCheckService>()
);
builder.Services.AddHostedService(sp => sp.GetRequiredService<UpdateCheckService>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler($"/{NavUri.ErrorPage}", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute($"/{NavUri.NotFoundPage}", createScopeForStatusCodePages: true);
app.UseStatusCodePagesWithReExecute($"/{NavUri.NotFoundPage}", createScopeForStatusCodePages: true);

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseForwardedHeaders(); // Must be before UseHttpsRedirection

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.UseRequestLocalization();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var modelDiscoveryService = services.GetRequiredService<IModelDiscoveryService>();
        await DbInitializer.InitializeAsync(
            context,
            userManager,
            roleManager,
            modelDiscoveryService
        );
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.UseStaticFiles();
app.MapStaticAssets();

// Add logout endpoint
app.MapPost(
    $"/{NavUri.LogoutPage}",
    async (SignInManager<User> signInManager) =>
    {
        await signInManager.SignOutAsync();
        return Results.Redirect($"/{NavUri.LoginPage}");
    }
);

// Add login endpoint
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
            {
                return Results.Redirect("/");
            }
            if (result.IsLockedOut)
            {
                return Results.Redirect($"/{NavUri.LoginPage}?error=AccountLocked");
            }
            return Results.Redirect($"/{NavUri.LoginPage}?error=InvalidLoginAttempt");
        }
    )
    .DisableAntiforgery(); // Disable antiforgery for simplicity in this demo, but recommended for production

// Add register endpoint
app.MapPost(
        "/perform-register",
        async (
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            [FromForm] string email,
            [FromForm] string password,
            [FromForm] string confirmPassword
        ) =>
        {
            if (password != confirmPassword)
            {
                return Results.Redirect($"/{NavUri.RegisterPage}?error=Passwords do not match");
            }

            var user = new User { UserName = email, Email = email };
            var result = await userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Assign User role to new registrations
                await userManager.AddToRoleAsync(user, Roles.User);

                // Create empty profile for new user
                var profile = new CandidateProfile
                {
                    UserId = user.Id,
                    FullName = email.Split('@')[0], // Default name from email
                    Email = email,
                    Skills = [],
                    WorkExperience = [],
                    Educations = [],
                };

                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.CandidateProfiles.Add(profile);
                await dbContext.SaveChangesAsync();

                await signInManager.SignInAsync(user, isPersistent: false);
                return Results.Redirect("/");
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Results.Redirect($"/{NavUri.RegisterPage}?error={Uri.EscapeDataString(errors)}");
        }
    )
    .DisableAntiforgery();

// External login challenge endpoint
app.MapGet(
    "/external-login/{provider}",
    async (string provider, SignInManager<User> signInManager, HttpContext httpContext) =>
    {
        // Sign out any existing user to ensure fresh OAuth flow
        await signInManager.SignOutAsync();

        // Clear any external authentication cookies to prevent caching
        await httpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        const string redirectUrl = "/external-login-callback";
        var properties = signInManager.ConfigureExternalAuthenticationProperties(
            provider,
            redirectUrl
        );

        // Force fresh login prompt for the selected provider
        properties.Items["prompt"] = "select_account";

        return Results.Challenge(properties, [provider]);
    }
);

// External login callback endpoint
app.MapGet(
    "/external-login-callback",
    async (
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        IServiceProvider serviceProvider
    ) =>
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            return Results.Redirect($"/{NavUri.LoginPage}?error=External login failed");
        }

        // Try to sign in with existing external login
        var signInResult = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true
        );

        if (signInResult.Succeeded)
        {
            return Results.Redirect("/");
        }
        if (signInResult.IsLockedOut)
        {
            return Results.Redirect($"/{NavUri.LoginPage}?error=AccountLocked");
        }

        // Create new user if not exists
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            return Results.Redirect(
                $"/{NavUri.LoginPage}?error=Email not provided by external provider"
            );
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return Results.Redirect(
                    $"/{NavUri.LoginPage}?error={Uri.EscapeDataString(errors)}"
                );
            }

            // Assign User role to new external registrations
            await userManager.AddToRoleAsync(user, Roles.User);

            // Create empty profile for new external user
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

        // Add external login to user
        await userManager.AddLoginAsync(user, info);

        // Sign in the user
        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Redirect("/");
    }
);

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Add version endpoint for auto-refresh
app.MapGet(
        "/api/version",
        (IUpdateCheckService updateCheckService) =>
        {
            var version =
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? "1.0.0.0";
            var isUpdateAvailable = updateCheckService.IsUpdateAvailable;
            var newVersionTag = updateCheckService.NewVersionTag;
            var isUpdateScheduled = updateCheckService.IsUpdateScheduled;
            var scheduledUpdateTime = updateCheckService.ScheduledUpdateTime;
            double? secondsRemaining = null;
            if (scheduledUpdateTime.HasValue)
            {
                secondsRemaining = (scheduledUpdateTime.Value - DateTime.UtcNow).TotalSeconds;
            }

            return Results.Ok(
                new
                {
                    version,
                    isUpdateAvailable,
                    newVersionTag,
                    isUpdateScheduled,
                    scheduledUpdateTime,
                    secondsRemaining, // Add relative time
                }
            );
        }
    )
    .AllowAnonymous(); // Allow polling without auth

// Add schedule-update endpoint (starts server-side countdown)
app.MapPost(
        "/api/schedule-update",
        (
            IUpdateCheckService updateCheckService,
            IWebHostEnvironment _env,
            ILogger<Program> logger
        ) =>
        {
            if (_env.IsDevelopment())
            {
                return Results.BadRequest("Scheduled updates are disabled in Development.");
            }
            logger.LogWarning("Manual update schedule requested via API.");
            updateCheckService.ScheduleUpdate(180); // 3 minutes

            // Short wait to ensure time is set if lock contention (rare)
            for (
                var attempt = 0;
                updateCheckService.ScheduledUpdateTime == null && attempt < 5;
                attempt++
            )
            {
                Thread.Sleep(50);
            }

            double? seconds = null;
            if (updateCheckService.ScheduledUpdateTime.HasValue)
            {
                seconds = (
                    updateCheckService.ScheduledUpdateTime.Value - DateTime.UtcNow
                ).TotalSeconds;
            }

            return Results.Ok(
                new
                {
                    scheduledUpdateTime = updateCheckService.ScheduledUpdateTime,
                    secondsRemaining = seconds,
                }
            );
        }
    )
    .RequireAuthorization();

// Add update trigger endpoint (immediate, for emergencies or internal use)
app.MapPost(
        "/api/trigger-update",
        async (IUpdateCheckService updateCheckService) =>
        {
            var success = await updateCheckService.TriggerUpdateAsync();
            return success ? Results.Ok() : Results.Problem("Failed to trigger update");
        }
    )
    .RequireAuthorization();

// Add culture set endpoint
app.MapGet(
    "/culture/set",
    (string culture, string redirectUri, HttpContext httpContext) =>
    {
        if (culture != null)
        {
            httpContext.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture))
            );
        }

        return Results.Redirect(redirectUri);
    }
);

app.Run();
