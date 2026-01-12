namespace AiCV.Infrastructure.Services;

public interface IUpdateCheckService
{
    bool IsUpdateAvailable { get; }
    string? NewVersionDigest { get; }
    string? NewVersionTag { get; }
    DateTime? ScheduledUpdateTime { get; }
    bool IsUpdateScheduled { get; }
    void ScheduleUpdate(int delaySeconds = 300);
    void CancelScheduledUpdate();
    Task<bool> TriggerUpdateAsync();
}

public class UpdateCheckService : BackgroundService, IUpdateCheckService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UpdateCheckService> _logger;
    private readonly IHostEnvironment _environment;
    private readonly string _repository;
    private readonly string _tag = "latest";
    private readonly string _watchtowerToken;
    private string? _currentDigest;
    private string? _newVersionDigest;
    private string? _newVersionTag;
    private bool _isUpdateAvailable;

    // Server-side scheduling fields
    private DateTime? _scheduledUpdateTime;
    private CancellationTokenSource? _scheduledUpdateCts;
    private readonly Lock _scheduleLock = new();

    public bool IsUpdateAvailable => _isUpdateAvailable;
    public string? NewVersionDigest => _newVersionDigest;
    public string? NewVersionTag => _newVersionTag;
    public DateTime? ScheduledUpdateTime => _scheduledUpdateTime;
    public bool IsUpdateScheduled =>
        _scheduledUpdateTime.HasValue && _scheduledUpdateTime > DateTime.UtcNow;

    public UpdateCheckService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<UpdateCheckService> logger,
        IHostEnvironment environment
    )
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
        _repository = _configuration["DOCKER_REPOSITORY"] ?? "timi74/aicv";
        _watchtowerToken = _configuration["WATCHTOWER_HTTP_API_TOKEN"] ?? string.Empty;
    }

    /// <summary>
    /// Schedules an update to trigger after the specified delay.
    /// Once scheduled, the update WILL happen regardless of client state.
    /// </summary>
    public void ScheduleUpdate(int delaySeconds = 300)
    {
        lock (_scheduleLock)
        {
            // If already scheduled, don't reset the timer
            if (_scheduledUpdateTime.HasValue && _scheduledUpdateTime > DateTime.UtcNow)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Update already scheduled for {ScheduledTime}. Not resetting.",
                        _scheduledUpdateTime
                    );
                }
                return;
            }

            _scheduledUpdateTime = DateTime.UtcNow.AddSeconds(delaySeconds);
            _scheduledUpdateCts?.Cancel();
            _scheduledUpdateCts = new CancellationTokenSource();

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Update scheduled for {ScheduledTime} (in {Seconds} seconds). This WILL proceed regardless of client state.",
                    _scheduledUpdateTime,
                    delaySeconds
                );
            }

            // Start background task to trigger update when time comes
            _ = ExecuteScheduledUpdateAsync(_scheduledUpdateCts.Token);
        }
    }

    /// <summary>
    /// Cancels a scheduled update (only if needed for emergencies).
    /// </summary>
    public void CancelScheduledUpdate()
    {
        lock (_scheduleLock)
        {
            if (_scheduledUpdateCts != null)
            {
                _logger.LogWarning("Scheduled update cancelled.");
                _scheduledUpdateCts.Cancel();
                _scheduledUpdateCts = null;
                _scheduledUpdateTime = null;
            }
        }
    }

    private async Task ExecuteScheduledUpdateAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!_scheduledUpdateTime.HasValue)
                return;

            var delay = _scheduledUpdateTime.Value - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Waiting {Delay} before triggering scheduled update...",
                        delay
                    );
                }
                await Task.Delay(delay, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            _logger.LogWarning("Scheduled update time reached. Triggering Watchtower now!");
            await TriggerUpdateAsync();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scheduled update was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scheduled update execution");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation("UpdateCheckService is disabled in Development environment.");
            return;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "UpdateCheckService starting for repository: {Repository}",
                _repository
            );
        }

        // Initial check to establish baseline
        _currentDigest = await GetLatestDigestAsync();
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Initial image digest: {Digest}", _currentDigest ?? "unknown");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for interval (default 5 minutes)
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                var latestDigest = await GetLatestDigestAsync();

                if (
                    latestDigest != null
                    && _currentDigest != null
                    && latestDigest != _currentDigest
                )
                {
                    if (!_isUpdateAvailable)
                    {
                        // Try to find semantic version (e.g. 1.0.5) matching this digest
                        var semanticVersion = await GetSemanticVersionByDigestAsync(latestDigest);
                        _newVersionTag = semanticVersion ?? "latest";

                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning(
                                "New version detected on registry! Digest: {Digest}, Version: {Version}",
                                latestDigest,
                                _newVersionTag
                            );
                        }
                        _isUpdateAvailable = true;
                        _newVersionDigest = latestDigest;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates on Docker Hub");
            }
        }
    }

    private async Task<string?> GetLatestDigestAsync()
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "AiCV-UpdateChecker");

            // Format: timi74/aicv -> https://hub.docker.com/v2/repositories/timi74/aicv/tags/latest
            var url = $"https://hub.docker.com/v2/repositories/{_repository}/tags/{_tag}";
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("digest", out var digestProp))
                {
                    return digestProp.GetString();
                }
            }
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(ex, "Failed to fetch digest from Docker Hub");
            }
        }
        return null;
    }

    private async Task<string?> GetSemanticVersionByDigestAsync(string digest)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            // Fetch recent tags to find one that matches the digest
            var url = $"https://hub.docker.com/v2/repositories/{_repository}/tags?page_size=20";
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("results", out var results))
                {
                    foreach (var tag in results.EnumerateArray())
                    {
                        var name = tag.GetProperty("name").GetString();
                        if (name == "latest")
                            continue;

                        // Check direct digest match
                        if (
                            tag.TryGetProperty("digest", out var tagDigest)
                            && tagDigest.GetString() == digest
                        )
                        {
                            return name;
                        }

                        // Check matches in 'images' array (multi-arch)
                        if (
                            tag.TryGetProperty("images", out var images)
                            && images.ValueKind == JsonValueKind.Array
                        )
                        {
                            foreach (var img in images.EnumerateArray())
                            {
                                if (
                                    img.TryGetProperty("digest", out var imgDigest)
                                    && imgDigest.GetString() == digest
                                )
                                {
                                    return name;
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(ex, "Failed to resolve semantic version from digest");
            }
        }
        return null;
    }

    public async Task<bool> TriggerUpdateAsync()
    {
        if (string.IsNullOrEmpty(_watchtowerToken))
        {
            _logger.LogError("Cannot trigger update: WATCHTOWER_HTTP_API_TOKEN is not configured.");
            return false;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _watchtowerToken
            );

            // Watchtower API is typically at http://watchtower:8080/v1/update
            // In our compose, the service name is 'watchtower'
            var response = await client.GetAsync("http://aicv-watchtower:8080/v1/update");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully signaled Watchtower to perform update.");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(
                        "Failed to signal Watchtower. Status: {Status}, Error: {Error}",
                        response.StatusCode,
                        error
                    );
                }
            }
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Error sending signal to Watchtower");
            }
        }
        return false;
    }
}
