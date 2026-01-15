namespace AiCV.Web.Components.Shared;

public partial class UpdateBanner
{
    private bool _showBanner = false;
    private bool _isInstalling = false;
    private int _secondsRemaining = 0;
    private string? _newVersionTag;
    private string? _currentVersion;
    private System.Timers.Timer? _pollTimer;
    private System.Timers.Timer? _countdownTimer;
    private bool _disposed = false;
    private string? _baseUri;

    protected override async Task OnInitializedAsync()
    {
        // Get the base URI from NavigationManager for API calls
        _baseUri = Navigation.BaseUri.TrimEnd('/');

        // Start polling for updates
        _pollTimer = new System.Timers.Timer(3000); // Check every 3 seconds
        _pollTimer.Elapsed += async (sender, e) => await CheckForUpdates();
        _pollTimer.AutoReset = true;
        _pollTimer.Start();

        // Initial check
        await CheckForUpdates();
    }

    private async Task CheckForUpdates()
    {
        if (_disposed || string.IsNullOrEmpty(_baseUri))
            return;

        try
        {
            using var http = HttpClientFactory.CreateClient();
            var response = await http.GetFromJsonAsync<VersionResponse>($"{_baseUri}/api/version");
            if (response == null)
                return;

            // Store current version on first check
            _currentVersion ??= response.Version;

            // Check if version changed (update was applied) - reload
            if (_currentVersion != response.Version)
            {
                // Version changed! Reload the page
                await InvokeAsync(() => Navigation.NavigateTo(Navigation.Uri, forceLoad: true));
                return;
            }

            // Check if update is scheduled
            if (response.IsUpdateScheduled && response.SecondsRemaining > 0)
            {
                _secondsRemaining = (int)response.SecondsRemaining;
                _newVersionTag = response.NewVersionTag;

                if (!_showBanner)
                {
                    _showBanner = true;
                    StartCountdown();
                }

                await InvokeAsync(StateHasChanged);
            }
            else if (response.IsUpdateAvailable && !_showBanner)
            {
                // Schedule update on server
                await ScheduleUpdate();
            }
        }
        catch
        {
            // If we're installing and can't reach server, that's expected
            if (_isInstalling)
            {
                // Keep showing installing state
            }
        }
    }

    private async Task ScheduleUpdate()
    {
        if (string.IsNullOrEmpty(_baseUri))
            return;

        try
        {
            using var http = HttpClientFactory.CreateClient();
            var response = await http.PostAsync($"{_baseUri}/api/schedule-update", null);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<ScheduleResponse>();
                if (data != null)
                {
                    _secondsRemaining = (int)(data.SecondsRemaining ?? 180);
                    _showBanner = true;
                    StartCountdown();
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        catch
        {
            // Ignore scheduling errors
        }
    }

    private void StartCountdown()
    {
        _countdownTimer?.Stop();
        _countdownTimer?.Dispose();

        _countdownTimer = new System.Timers.Timer(1000);
        _countdownTimer.Elapsed += (sender, e) =>
        {
            if (_disposed)
                return;

            _secondsRemaining--;

            if (_secondsRemaining <= 0)
            {
                _isInstalling = true;
                _countdownTimer?.Stop();
            }

            InvokeAsync(StateHasChanged);
        };
        _countdownTimer.AutoReset = true;
        _countdownTimer.Start();
    }

    private static string FormatTime(int seconds)
    {
        if (seconds < 0)
            seconds = 0;
        var minutes = seconds / 60;
        var secs = seconds % 60;
        return $"{minutes}:{secs:D2}";
    }

    public void Dispose()
    {
        _disposed = true;
        _pollTimer?.Stop();
        _pollTimer?.Dispose();
        _countdownTimer?.Stop();
        _countdownTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    private class VersionResponse
    {
        public string? Version { get; set; }
        public bool IsUpdateAvailable { get; set; }
        public string? NewVersionTag { get; set; }
        public bool IsUpdateScheduled { get; set; }
        public double? SecondsRemaining { get; set; }
    }

    private class ScheduleResponse
    {
        public double? SecondsRemaining { get; set; }
    }
}
