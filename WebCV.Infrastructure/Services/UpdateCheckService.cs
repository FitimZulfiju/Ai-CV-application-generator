using Microsoft.Extensions.Hosting;

namespace WebCV.Infrastructure.Services;

public interface IUpdateCheckService
{
    bool IsUpdateAvailable { get; }
    string? NewVersionDigest { get; }
    Task<bool> TriggerUpdateAsync();
}

public class UpdateCheckService : BackgroundService, IUpdateCheckService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UpdateCheckService> _logger;
    private readonly string _repository;
    private readonly string _tag = "latest";
    private readonly string _watchtowerToken;
    private string? _currentDigest;
    private string? _newVersionDigest;
    private bool _isUpdateAvailable;

    public bool IsUpdateAvailable => _isUpdateAvailable;
    public string? NewVersionDigest => _newVersionDigest;

    public UpdateCheckService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<UpdateCheckService> logger
    )
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _repository = _configuration["DOCKER_REPOSITORY"] ?? "timi74/webcv";
        _watchtowerToken = _configuration["WATCHTOWER_HTTP_API_TOKEN"] ?? string.Empty;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "UpdateCheckService starting for repository: {Repository}",
            _repository
        );

        // Initial check to establish baseline
        _currentDigest = await GetLatestDigestAsync();
        _logger.LogInformation("Initial image digest: {Digest}", _currentDigest ?? "unknown");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for interval (default 1 minute)
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                var latestDigest = await GetLatestDigestAsync();

                if (
                    latestDigest != null
                    && _currentDigest != null
                    && latestDigest != _currentDigest
                )
                {
                    if (!_isUpdateAvailable)
                    {
                        _logger.LogWarning(
                            "New version detected on registry! Digest: {Digest}",
                            latestDigest
                        );
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
            client.DefaultRequestHeaders.Add("User-Agent", "WebCV-UpdateChecker");

            // Format: timi74/webcv -> https://hub.docker.com/v2/repositories/timi74/webcv/tags/latest
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
            _logger.LogDebug(ex, "Failed to fetch digest from Docker Hub");
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
            var response = await client.GetAsync("http://webcv-watchtower:8080/v1/update");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully signaled Watchtower to perform update.");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Failed to signal Watchtower. Status: {Status}, Error: {Error}",
                    response.StatusCode,
                    error
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending signal to Watchtower");
        }
        return false;
    }
}
