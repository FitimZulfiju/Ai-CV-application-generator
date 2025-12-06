var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddMudServices();

// Add authorization
builder.Services.AddAuthorizationBuilder()
                        // Add authorization
                        .SetFallbackPolicy(null);

// Database 

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Database=WebCV;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString), ServiceLifetime.Scoped, ServiceLifetime.Singleton);

// Add Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
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

// Configure cookie authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/login";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Register Application Services
builder.Services.AddScoped<ICVService, CVService>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IJobPostScraper, JobPostScraper>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();

// Configure named HttpClient for Local AI with longer timeout
builder.Services.AddHttpClient("LocalAI", client =>
{
    client.Timeout = TimeSpan.FromMinutes(10);
});

// Configure Forwarded Headers for Docker/Proxy scenarios
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddScoped<IAIServiceFactory, AIServiceFactory>();
builder.Services.AddScoped<IClipboardService, ClipboardService>();
builder.Services.AddScoped<IJobApplicationOrchestrator, JobApplicationOrchestrator>();
builder.Services.AddScoped<ILoadingService, LoadingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseForwardedHeaders(); // Must be before UseHttpsRedirection

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        await DbInitializer.InitializeAsync(context, userManager);
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
app.MapPost("/logout", async (SignInManager<User> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});

// Add login endpoint
app.MapPost("/perform-login", async (SignInManager<User> signInManager, [FromForm] string email, [FromForm] string password, [FromForm] bool? rememberMe) =>
{
    var result = await signInManager.PasswordSignInAsync(email, password, rememberMe ?? false, lockoutOnFailure: false);
    if (result.Succeeded)
    {
        return Results.Redirect("/");
    }
    return Results.Redirect("/login?error=Invalid login attempt");
}).DisableAntiforgery(); // Disable antiforgery for simplicity in this demo, but recommended for production

// Add register endpoint
app.MapPost("/perform-register", async (UserManager<User> userManager, SignInManager<User> signInManager, [FromForm] string email, [FromForm] string password, [FromForm] string confirmPassword) =>
{
    if (password != confirmPassword)
    {
        return Results.Redirect("/register?error=Passwords do not match");
    }

    var user = new User { UserName = email, Email = email };
    var result = await userManager.CreateAsync(user, password);

    if (result.Succeeded)
    {
        // Create empty profile for new user
        var profile = new CandidateProfile
        {
            UserId = user.Id,
            FullName = email.Split('@')[0], // Default name from email
            Email = email,
            Skills = [],
            WorkExperience = [],
            Educations = []
        };

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.CandidateProfiles.Add(profile);
        await dbContext.SaveChangesAsync();

        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Redirect("/");
    }

    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
    return Results.Redirect($"/register?error={Uri.EscapeDataString(errors)}");
}).DisableAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
