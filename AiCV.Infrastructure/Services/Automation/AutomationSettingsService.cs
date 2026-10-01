namespace AiCV.Infrastructure.Services.Automation;

public class AutomationSettingsService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IOptions<AutomationOptions> options,
    ILogger<AutomationSettingsService> logger
) : IAutomationSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
    private readonly IOptions<AutomationOptions> _options = options;
    private readonly ILogger<AutomationSettingsService> _logger = logger;

    private static readonly (string Query, string Location)[] DefaultQueries =
    [
        (".NET backend udvikler", "København"),
        ("ASP.NET Core udvikler", "København"),
        ("Azure developer", "København"),
        ("C# backend developer", "Storkøbenhavn"),
        ("software engineer .NET", "København"),
        ("full stack .NET udvikler", "Storkøbenhavn"),
    ];

    public async Task<AutomationSettings?> GetForUserAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context
            .AutomationSettings
            .AsNoTracking()
            .Include(s => s.Queries)
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<AutomationSettings?> GetOrCreateAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context
            .AutomationSettings
            .Include(s => s.Queries)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (existing is not null)
        {
            return existing;
        }

        var settings = new AutomationSettings
        {
            UserId = userId,
            IsEnabled = false,
            CronExpression = _options.Value.DefaultCron,
            MaxApplicationsPerRun = _options.Value.DefaultMaxApplicationsPerRun,
            Providers = "Jobindex",
            Queries = [.. DefaultQueries
                .Select(q => new AutomationQuery
                {
                    Query = q.Query,
                    Provider = "Jobindex",
                    Location = q.Location,
                    Region = CopenhagenRegion.RegionName,
                    IsActive = true,
                    JobAgeDays = 7,
                    MaxResults = 20,
                })],
            NextRunUtc = ComputeNextRun(_options.Value.DefaultCron, DateTime.UtcNow, _logger),
        };

        context.AutomationSettings.Add(settings);
        await context.SaveChangesAsync();

        return settings;
    }

    public async Task<AutomationSettings> UpdateAsync(AutomationSettings settings, string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context
            .AutomationSettings
            .Include(s => s.Queries)
            .FirstOrDefaultAsync(s => s.UserId == userId)
            ?? throw new InvalidOperationException($"Automation settings not found for user '{userId}'.");

        existing.IsEnabled = settings.IsEnabled;
        existing.CronExpression = settings.CronExpression;
        existing.MaxApplicationsPerRun = Math.Clamp(settings.MaxApplicationsPerRun, 1, 100);
        existing.Providers = settings.Providers;

        var incoming = settings.Queries ?? [];
        var existingById = existing.Queries.ToDictionary(q => q.Id);

        context.AutomationQueries.RemoveRange(existing.Queries.Where(q => !incoming.Any(i => i.Id == q.Id)));

        foreach (var query in incoming)
        {
            if (query.Id > 0 && existingById.TryGetValue(query.Id, out var tracked))
            {
                tracked.Query = query.Query;
                tracked.Provider = query.Provider;
                tracked.Location = query.Location;
                tracked.JobAgeDays = Math.Clamp(query.JobAgeDays, 1, 365);
                tracked.MaxResults = Math.Clamp(query.MaxResults, 1, 100);
                tracked.IsActive = query.IsActive;
            }
            else
            {
                existing.Queries.Add(new AutomationQuery
                {
                    Query = query.Query,
                    Provider = query.Provider,
                    Location = query.Location,
                    JobAgeDays = Math.Clamp(query.JobAgeDays, 1, 365),
                    MaxResults = Math.Clamp(query.MaxResults, 1, 100),
                    IsActive = query.IsActive,
                });
            }
        }

        existing.NextRunUtc = ComputeNextRun(existing.CronExpression, DateTime.UtcNow, _logger);
        await context.SaveChangesAsync();

        return existing;
    }

    public async Task<bool> DeleteAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context
            .AutomationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (existing is null)
        {
            return false;
        }

        context.AutomationSettings.Remove(existing);
        await context.SaveChangesAsync();
        return true;
    }

    private static readonly TimeZoneInfo ScheduleTimeZone = ResolveScheduleTimeZone();

    private static TimeZoneInfo ResolveScheduleTimeZone()
    {
        string[] candidateIds = ["Europe/Copenhagen", "Romance Standard Time"];
        foreach (var id in candidateIds)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try the next id.
            }
            catch (InvalidTimeZoneException)
            {
                // Try the next id.
            }
        }

        return TimeZoneInfo.Utc;
    }

    internal static DateTime? ComputeNextRun(string cronExpression, DateTime nowUtc, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            logger?.LogWarning("Empty cron expression; cannot compute next run.");
            return null;
        }

        try
        {
            var schedule = CrontabSchedule.Parse(cronExpression);
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ScheduleTimeZone);
            var nextLocal = schedule.GetNextOccurrence(localNow);

            var guard = 0;
            while (ScheduleTimeZone.IsInvalidTime(nextLocal) && guard++ < 24)
            {
                nextLocal = nextLocal.AddHours(1);
            }

            return TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(nextLocal, DateTimeKind.Unspecified),
                ScheduleTimeZone);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(
                ex,
                "Invalid cron expression '{CronExpression}'; next run could not be computed.",
                cronExpression);
            return null;
        }
    }
}