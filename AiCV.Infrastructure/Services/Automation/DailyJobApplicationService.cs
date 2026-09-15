namespace AiCV.Infrastructure.Services.Automation;

public class DailyJobApplicationService(
    IServiceProvider serviceProvider,
    IOptions<AutomationOptions> options,
    ILogger<DailyJobApplicationService> logger
) : BackgroundService, IDailyJobApplicationService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IOptions<AutomationOptions> _options = options;
    private readonly ILogger<DailyJobApplicationService> _logger = logger;
    private readonly ConcurrentDictionary<string, byte> _runningUsers = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Daily job application service started.");

        var pollInterval = TimeSpan.FromSeconds(Math.Max(10, _options.Value.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueUsersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Daily job application poll iteration failed.");
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Daily job application service stopped.");
    }

    private async Task ProcessDueUsersAsync(CancellationToken cancellationToken)
    {
        List<string> dueUserIds;

        await using (var context = await _serviceProvider
            .GetRequiredService<IDbContextFactory<ApplicationDbContext>>()
            .CreateDbContextAsync(cancellationToken))
        {
            var now = DateTime.UtcNow;
            dueUserIds = await context
                .AutomationSettings
                .AsNoTracking()
                .Where(s => s.IsEnabled && s.NextRunUtc <= now)
                .Select(s => s.UserId)
                .ToListAsync(cancellationToken);
        }

        foreach (var userId in dueUserIds)
        {
            if (!_runningUsers.TryAdd(userId, 0))
            {
                continue;
            }

            try
            {
                await RunCoreAsync(userId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled automation run failed for user {UserId}", userId);
                await TryAdvanceScheduleAsync(userId, DateTime.UtcNow, cancellationToken);
            }
            finally
            {
                _runningUsers.TryRemove(userId, out _);
            }
        }
    }

    public Task<AutomationRunResult> RunForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return RunOnceAsync(userId, cancellationToken);
    }

    public async Task<AutomationRunResult> RunOnceAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!_runningUsers.TryAdd(userId, 0))
        {
            return new AutomationRunResult(false, 0, 0, 0, ["An automation run is already in progress."], DateTime.UtcNow);
        }

        try
        {
            return await RunCoreAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Automation run failed for user {UserId}", userId);
            return new AutomationRunResult(false, 0, 0, 0, [ex.Message], DateTime.UtcNow);
        }
        finally
        {
            _runningUsers.TryRemove(userId, out _);
        }
    }

    private async Task<AutomationRunResult> RunCoreAsync(string userId, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var startedAt = DateTime.UtcNow;

        await using var scope = _serviceProvider.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        var contextFactory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var settingsService = sp.GetRequiredService<IAutomationSettingsService>();
        var cvService = sp.GetRequiredService<ICVService>();
        var configService = sp.GetRequiredService<IUserAIConfigurationService>();
        var providerFactory = sp.GetRequiredService<JobSearchProviderFactory>();
        var matcher = sp.GetRequiredService<IJobMatcher>();
        var orchestrator = sp.GetRequiredService<IJobApplicationOrchestrator>();
        var emailSender = sp.GetRequiredService<ISmtpEmailSender>();

        var settings = await settingsService.GetForUserAsync(userId);
        if (settings is null)
        {
            return new AutomationRunResult(false, 0, 0, 0, ["Automation settings not found."], startedAt);
        }

        var profile = await cvService.GetProfileAsync(userId);
        if (profile is null)
        {
            await AdvanceScheduleAsync(contextFactory, userId, startedAt, cancellationToken);
            return new AutomationRunResult(false, 0, 0, 0, ["Candidate profile not found. Create a profile first."], startedAt);
        }

        var aiConfig = await configService.GetActiveConfigurationAsync(userId);
        if (aiConfig is null)
        {
            errors.Add("No active AI configuration. Please activate an AI provider in settings.");
        }

        var allJobs = new List<JobSearchResult>();
        var activeQueries = settings.Queries.Where(q => q.IsActive).ToList();

        foreach (var query in activeQueries)
        {
            if (string.IsNullOrWhiteSpace(query.Query))
            {
                continue;
            }

            try
            {
                var provider = providerFactory.GetProvider(query.Provider) ?? providerFactory.GetProvider("Jobindex");
                if (provider is null)
                {
                    errors.Add($"No search provider available for '{query.Provider}'.");
                    continue;
                }

                var results = await provider.SearchAsync(
                    new JobSearchQuery(
                        query.Query,
                        query.Provider,
                        query.Location,
                        query.Region,
                        Math.Max(1, query.MaxResults),
                        Math.Clamp(query.JobAgeDays, 1, 365)),
                    cancellationToken);

                allJobs.AddRange(results);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Search failed for query '{Query}' (user {UserId})", query.Query, userId);
                errors.Add($"Search '{query.Query}' failed: {ex.Message}");
            }
        }

        var distinctJobs = allJobs
            .GroupBy(j => j.Url, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        await using (var ctx = await contextFactory.CreateDbContextAsync(cancellationToken))
        {
            var savedUrls = await ctx
                .GeneratedApplications
                .Where(a => a.UserId == userId && a.JobPosting != null)
                .Select(a => a.JobPosting!.Url)
                .ToListAsync(cancellationToken);

            var savedSet = new HashSet<string>(savedUrls, StringComparer.OrdinalIgnoreCase);
            distinctJobs = [.. distinctJobs.Where(j => !savedSet.Contains(j.Url))];
        }

        var matched = await matcher.MatchAsync(profile, distinctJobs, settings.MaxApplicationsPerRun, cancellationToken);

        if (aiConfig is null)
        {
            await AdvanceScheduleAsync(contextFactory, userId, startedAt, cancellationToken);
            await SendSummaryEmailAsync(emailSender, profile, distinctJobs.Count, matched.Count, 0, errors, _logger);
            return new AutomationRunResult(false, distinctJobs.Count, matched.Count, 0, errors, startedAt);
        }

        var generated = 0;
        foreach (var matchedJob in matched)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var jobPosting = await orchestrator.FetchJobDetailsAsync(matchedJob.Job.Url);
                jobPosting.ApplyUrl = matchedJob.Job.ApplyUrl;

                var (coverLetter, resumeResult, applicationEmail) =
                    await orchestrator.GenerateApplicationAsync(
                        userId,
                        aiConfig.Provider,
                        profile,
                        jobPosting,
                        aiConfig.ModelId);

                await orchestrator.SaveApplicationAsync(
                    userId,
                    jobPosting,
                    profile,
                    coverLetter,
                    resumeResult.Profile,
                    applicationEmail,
                    CvTemplates.Professional,
                    ApplicationStatus.PendingReview);

                generated++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Application generation failed for job '{JobUrl}' (user {UserId})", matchedJob.Job.Url, userId);
                errors.Add($"Application for '{matchedJob.Job.Title}' failed: {ex.Message}");
            }
        }

        await AdvanceScheduleAsync(contextFactory, userId, startedAt, cancellationToken);
        await SendSummaryEmailAsync(emailSender, profile, distinctJobs.Count, matched.Count, generated, errors, _logger);

        return new AutomationRunResult(
            errors.Count == 0,
            distinctJobs.Count,
            matched.Count,
            generated,
            errors,
            DateTime.UtcNow);
    }

    private static async Task AdvanceScheduleAsync(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        string userId,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync(cancellationToken);
        var tracked = await ctx.AutomationSettings.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (tracked is null)
        {
            return;
        }

        tracked.LastRunUtc = startedAt;
        tracked.NextRunUtc = AutomationSettingsService.ComputeNextRun(tracked.CronExpression, DateTime.UtcNow)
            ?? DateTime.UtcNow.AddDays(1);

        await ctx.SaveChangesAsync(cancellationToken);
    }

    private async Task TryAdvanceScheduleAsync(string userId, DateTime startedAt, CancellationToken cancellationToken)
    {
        try
        {
            var contextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            await AdvanceScheduleAsync(contextFactory, userId, startedAt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to advance automation schedule for user {UserId}", userId);
        }
    }

    private static async Task SendSummaryEmailAsync(
        ISmtpEmailSender emailSender,
        CandidateProfile profile,
        int jobsFound,
        int jobsMatched,
        int applicationsGenerated,
        List<string> errors,
        ILogger? logger = null)
    {
        try
        {
            var errorHtml = errors.Count == 0
                ? "<p style=\"color:#16a34a;\">No errors.</p>"
                : "<p style=\"color:#dc2626;\">" + string.Join("<br/>", errors.Select(System.Net.WebUtility.HtmlEncode)) + "</p>";

            var body = $@"
<p>Hello {System.Net.WebUtility.HtmlEncode(profile.FullName)},</p>
<p>Your daily job automation run completed.</p>
<ul>
    <li><strong>Jobs found:</strong> {jobsFound}</li>
    <li><strong>Jobs matched:</strong> {jobsMatched}</li>
    <li><strong>Applications generated:</strong> {applicationsGenerated}</li>
</ul>
{errorHtml}
<p>Review your pending applications in the <em>My Applications</em> area.</p>";

            await emailSender.SendAutomationSummaryAsync(
                profile.Email,
                "Your Daily Job Automation Summary - AiCV",
                "Daily Job Automation Summary",
                body);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to send automation summary email.");
        }
    }
}