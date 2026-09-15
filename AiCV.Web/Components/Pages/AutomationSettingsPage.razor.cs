namespace AiCV.Web.Components.Pages;

public partial class AutomationSettingsPage
{
    private static string PresetDaily6 => "0 6 * * *";
    private static string PresetDaily8 => "0 8 * * *";
    private static string PresetWeekdays7 => "0 7 * * 1-5";
    private static string PresetCustom => "__custom__";

    private AutomationSettings? _settings;
    private bool _isLoading = true;
    private bool _isRunning;
    private string _userId = string.Empty;
    private string _selectedPreset = "0 6 * * *";

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            _userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (!string.IsNullOrEmpty(_userId))
            {
                _settings = await AutomationSettingsService.GetOrCreateAsync(_userId);
                if (_settings is not null)
                {
                    _selectedPreset = _settings.CronExpression;
                }
            }
        }

        _isLoading = false;
    }

    private void ApplyPreset()
    {
        if (_settings is null || _selectedPreset == "__custom__")
        {
            return;
        }
        _settings.CronExpression = _selectedPreset;
        StateHasChanged();
    }

    private void AddQuery()
    {
        if (_settings is null)
        {
            return;
        }
        _settings.Queries.Add(new AutomationQuery
        {
            Query = string.Empty,
            Provider = "Jobindex",
            IsActive = true,
            JobAgeDays = 7,
            MaxResults = 20
        });
    }

    private void RemoveQuery(AutomationQuery query)
    {
        _settings?.Queries.Remove(query);
    }

    private async Task SaveSettings()
    {
        if (_settings is null)
        {
            return;
        }

        try
        {
            _settings = await AutomationSettingsService.UpdateAsync(_settings, _userId);
            _selectedPreset = _settings.CronExpression;
            Snackbar.Add(Localizer["SettingsSaved"], Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"{Localizer["Error"]}: {ex.Message}", Severity.Error);
        }
    }

    private string _testDebugLog = string.Empty;

    private async Task RunTestNow()
    {
        if (_settings is null)
        {
            return;
        }

        _isRunning = true;
        _testDebugLog = "Starting test run...\n";
        StateHasChanged();

        try
        {
            // Persist current edits first so the test run uses them.
            _settings = await AutomationSettingsService.UpdateAsync(_settings, _userId);
            _testDebugLog += "Settings saved.\n";

            var result = await DailyJobApplicationService.RunOnceAsync(_userId);

            _testDebugLog += $"Success: {result.Success}\n";
            _testDebugLog += $"Jobs Found: {result.JobsFound}\n";
            _testDebugLog += $"Jobs Matched: {result.JobsMatched}\n";
            _testDebugLog += $"Applications Generated: {result.ApplicationsGenerated}\n";

            if (result.Errors.Count != 0)
            {
                _testDebugLog += "Errors:\n" + string.Join("\n", result.Errors) + "\n";
            }

            if (result.Success)
            {
                Snackbar.Add(Localizer["TestRunComplete"], Severity.Success);
            }
            else
            {
                Snackbar.Add(
                    string.Format(Localizer["TestRunCompleteWithErrors"].Value, result.ApplicationsGenerated, result.Errors.Count),
                    Severity.Warning);
            }

            _settings = await AutomationSettingsService.GetForUserAsync(_userId) ?? _settings;
        }
        catch (Exception ex)
        {
            _testDebugLog += $"CRITICAL ERROR: {ex.Message}\n{ex.StackTrace}\n";
            Snackbar.Add($"{Localizer["Error"]}: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isRunning = false;
            StateHasChanged();
        }
    }
}