// Load .env file for configuration (searches current and parent directories)
var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
while (currentDir != null)
{
    var envFile = Path.Combine(currentDir.FullName, ".env");
    var deployEnvFile = Path.Combine(currentDir.FullName, "deploy", ".env");

    if (File.Exists(envFile))
    {
        DotNetEnv.Env.Load(envFile);
        break;
    }

    if (File.Exists(deployEnvFile))
    {
        DotNetEnv.Env.Load(deployEnvFile);
        break;
    }

    currentDir = currentDir.Parent;
}

// Enable legacy timestamp behavior for PostgreSQL compatibility
// This allows DateTime with Kind=Unspecified to work with PostgreSQL
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

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
builder.Services.AddAuthorizationBuilder().SetFallbackPolicy(null);

// Database Provider Selection
// Set DB_PROVIDER environment variable to "PostgreSQL" or "SqlServer" (default)
var dbProvider = builder.Configuration["DB_PROVIDER"] ?? "SqlServer";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        // Try to build PostgreSQL connection string from individual variables
        var pgHost = builder.Configuration["PG_HOST"] ?? "localhost";
        var pgPort = builder.Configuration["PG_PORT"] ?? "5432";
        var pgDb = builder.Configuration["DB_NAME"] ?? "aicv_db";
        var pgUser = builder.Configuration["DB_USER"] ?? "sa";
        var pgPass = builder.Configuration["DB_PASSWORD"] ?? "";

        // Strip single quotes if present (common in .env files)
        if (pgPass.StartsWith('\'') && pgPass.EndsWith('\''))
            pgPass = pgPass[1..^1];

        if (
            pgHost.Equals("shared-postgres", StringComparison.OrdinalIgnoreCase)
            || pgHost.Equals("db", StringComparison.OrdinalIgnoreCase)
        )
        {
            pgHost = "localhost";
        }

        connectionString =
            $"Host={pgHost};Port={pgPort};Database={pgDb};Username={pgUser};Password={pgPass};";
    }
    else
    {
        // Try to build SQL Server connection string from individual variables
        var sqlServer = builder.Configuration["DB_SERVER"] ?? "localhost";
        var sqlDb = builder.Configuration["DB_NAME"] ?? "AiCV_db";
        var sqlUser = builder.Configuration["DB_USER"];
        var sqlPass = builder.Configuration["DB_PASSWORD"] ?? "";

        if (sqlPass.StartsWith('\'') && sqlPass.EndsWith('\''))
            sqlPass = sqlPass[1..^1];

        if (
            sqlServer.Equals("shared-sqlserver", StringComparison.OrdinalIgnoreCase)
            || sqlServer.Equals("db", StringComparison.OrdinalIgnoreCase)
        )
        {
            sqlServer = "localhost";
        }

        if (!string.IsNullOrEmpty(sqlUser))
        {
            connectionString =
                $"Server={sqlServer};Database={sqlDb};User Id={sqlUser};Password={sqlPass};TrustServerCertificate=True;MultipleActiveResultSets=true;";
        }
        else
        {
            connectionString =
                $"Server={sqlServer};Database={sqlDb};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
        }
    }
}

// Configure DbContext based on provider
Action<DbContextOptionsBuilder> configureDbContext = dbProvider.Equals(
    "PostgreSQL",
    StringComparison.OrdinalIgnoreCase
)
    ? options =>
        options
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly("AiCV.Migrations.PostgreSQL")
            )
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
    : options =>
        options
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly("AiCV.Migrations.SqlServer")
            )
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

builder.Services.AddDbContextFactory<ApplicationDbContext>(configureDbContext);
builder.Services.AddDbContext<ApplicationDbContext>(
    configureDbContext,
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
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
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
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
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
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
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
    // Use SameAsRequest to allow flexibility for proxies and local testing.
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = ".AiCV.Application";
});

// Explicitly configure external cookie to prevent "Session Expired" errors
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = ".AiCV.External";
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
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IModelDiscoveryService, ModelDiscoveryService>();
builder.Services.AddScoped<ClientPersistenceService>();

// Data Protection - Persist keys to file system to prevent API key loss on app restart
var keysPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
if (!Directory.Exists(keysPath))
{
    Directory.CreateDirectory(keysPath);
}

// Ensure Uploads and Images directories exist for user data persistence
var webRootPath = builder.Environment.WebRootPath;
if (string.IsNullOrEmpty(webRootPath))
{
    webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
}

var uploadsPath = Path.Combine(webRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

var imagesPath = Path.Combine(webRootPath, "images");
if (!Directory.Exists(imagesPath))
{
    Directory.CreateDirectory(imagesPath);
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

// Backup Services
builder.Services.AddScoped<IDbBackupRestoreService, DbBackupRestoreService>();
builder.Services.AddSingleton<IBackupService, BackupService>();
builder.Services.AddHostedService<BackupBackgroundService>();

// Database Initialization
builder.Services.AddScoped<IDbInitializer, AiCV.Infrastructure.Data.DbInitializer>();

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

// Note: Database initialization and seeding is now handled consistently at the end of the file
// using the DI-registered IDbInitializer service.

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

// Direct GET logout endpoint for Blazor components (avoids "Headers are read-only" error)
app.MapGet(
    "/logout-direct",
    async (SignInManager<User> signInManager, ILogger<Program> logger) =>
    {
        logger.LogInformation("Direct logout requested.");
        await signInManager.SignOutAsync();
        return Results.Redirect("/");
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

// Support GET login for auto-login after registration
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
        {
            return Results.Redirect("/");
        }
        return Results.Redirect($"/{NavUri.LoginPage}?error=InvalidLoginAttempt");
    }
);

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
        ILogger<Program> logger,
        HttpContext httpContext,
        IServiceProvider serviceProvider
    ) =>
    {
        // Debug logging to find out why info is null
        var cookies = httpContext.Request.Headers.Cookie.ToString();
        logger.LogWarning("External Login Callback. Cookies: {Cookies}", cookies);

        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            logger.LogWarning(
                "External login info is null. This usually means the correlation cookie was lost or expired."
            );
            return Results.Redirect($"/{NavUri.LoginPage}?error=ExternalLoginFailed");
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

        // Create new user or merge with existing one by email
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            return Results.Redirect($"/{NavUri.LoginPage}?error=ExternalEmailNotProvided");
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

        // Helper to avoid logging full email addresses (PII) to external logs
        static string? RedactEmail(string? email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return email;
            }

            var atIndex = email.IndexOf('@');
            if (atIndex <= 0)
            {
                // Not a standard email format; return a partially masked version
                return email.Length <= 2
                    ? new string('*', email.Length)
                    : email[0] + new string('*', email.Length - 1);
            }

            var local = email.Substring(0, atIndex);
            var domain = email.Substring(atIndex);

            if (local.Length <= 1)
            {
                return "*" + domain;
            }

            return local[0] + new string('*', local.Length - 1) + domain;
        }

        // Merge account: Add the external login if it doesn't exist
        var logins = await userManager.GetLoginsAsync(user);
        if (
            !logins.Any(l =>
                l.LoginProvider == info.LoginProvider && l.ProviderKey == info.ProviderKey
            )
        )
        {
            var addLoginResult = await userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                logger.LogError(
                    "Failed to add external login for user {Email}",
                    RedactEmail(email)
                );
            }
        }

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

// Initialize database and apply migrations
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        await dbInitializer.InitializeAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database initialization.");
    }
}

app.Run();
