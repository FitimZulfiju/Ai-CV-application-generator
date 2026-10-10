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
    private List<SearchProviderConfig> _providerConfigs = [];

    private Task<IEnumerable<string>> SearchProviders(string value, CancellationToken _)
    {
        var providers = SearchProvidersList.Select(p => p.ProviderName).ToList();
        providers.AddRange(_providerConfigs.Where(p => !string.IsNullOrWhiteSpace(p.Provider)).Select(p => p.Provider).Distinct());
        var uniqueProviders = providers.Distinct().ToList();

        if (string.IsNullOrWhiteSpace(value))
            return Task.FromResult<IEnumerable<string>>(uniqueProviders);

        return Task.FromResult<IEnumerable<string>>(
            uniqueProviders.Where(x => x.Contains(value, StringComparison.InvariantCultureIgnoreCase)));
    }

    private Task<IEnumerable<string>> SearchConfiguredProviders(string value, CancellationToken _)
    {
        var configured = SearchProvidersList.Select(p => p.ProviderName).ToList();
        configured.AddRange(_providerConfigs.Where(p => !string.IsNullOrWhiteSpace(p.Provider)).Select(p => p.Provider).Distinct());
        var uniqueConfigured = configured.Distinct().ToList();

        if (string.IsNullOrWhiteSpace(value))
            return Task.FromResult<IEnumerable<string>>(uniqueConfigured);

        return Task.FromResult<IEnumerable<string>>(
            uniqueConfigured.Where(x => x.Contains(value, StringComparison.InvariantCultureIgnoreCase)));
    }

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
                    if (!string.IsNullOrEmpty(_settings.SearchProviderSettingsJson))
                    {
                        try
                        {
                            _providerConfigs = System.Text.Json.JsonSerializer.Deserialize<List<SearchProviderConfig>>(_settings.SearchProviderSettingsJson) ?? [];
                        }
                        catch
                        {
                            _providerConfigs = [];
                        }
                    }
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

    private List<string> _newQueryTexts = [];
    private List<string> _newQueryLocations = [];
    private int _newQueryJobAgeDays = 7;
    private int _newQueryMaxResults = 20;
    private List<string> _newQueryProviders = [];

    private Task<IEnumerable<string>> SearchQueries(string value, CancellationToken _)
    {
        var existingTags = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        if (_settings?.Queries != null)
        {
            foreach (var q in _settings.Queries)
            {
                if (!string.IsNullOrWhiteSpace(q.Query))
                {
                    foreach (var part in q.Query.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        existingTags.Add(part);
                    }
                }
            }
        }

        existingTags.ExceptWith(_newQueryTexts);

        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult<IEnumerable<string>>(existingTags);
        }

        var matches = existingTags.Where(t => t.Contains(value, StringComparison.InvariantCultureIgnoreCase)).ToList();

        return Task.FromResult<IEnumerable<string>>(matches);
    }

    private Task<IEnumerable<string>> SearchLocations(string value, CancellationToken _)
    {
        var existingTags = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        if (_settings?.Queries != null)
        {
            foreach (var q in _settings.Queries)
            {
                if (!string.IsNullOrWhiteSpace(q.Location))
                {
                    foreach (var part in q.Location.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        existingTags.Add(part);
                    }
                }
            }
        }

        existingTags.ExceptWith(_newQueryLocations);

        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult<IEnumerable<string>>(existingTags);
        }

        var matches = existingTags.Where(t => t.Contains(value, StringComparison.InvariantCultureIgnoreCase)).ToList();

        return Task.FromResult<IEnumerable<string>>(matches);
    }

    private void AddQueryFromForm()
    {
        if (_settings is null) return;

        if (_newQueryTexts.Count == 0)
        {
            Snackbar.Add(Localizer["Please enter at least one query keyword."], Severity.Warning);
            return;
        }

        string combinedQuery = string.Join(", ", _newQueryTexts);
        string combinedLocation = string.Join(", ", _newQueryLocations);
        string combinedProvider = _newQueryProviders.Count != 0 ? string.Join(", ", _newQueryProviders) : "Jobindex";

        _settings.Queries.Add(new AutomationQuery
        {
            Query = combinedQuery,
            Provider = combinedProvider,
            Location = combinedLocation,
            MaxResults = _newQueryMaxResults,
            JobAgeDays = _newQueryJobAgeDays,
            IsActive = true
        });

        // Reset the form
        _newQueryTexts.Clear();
        _newQueryLocations.Clear();
        _newQueryProviders.Clear();
    }

    private void RemoveQuery(AutomationQuery query)
    {
        _settings?.Queries.Remove(query);
    }

    private SearchProviderConfig _newProviderConfig = new() { Provider = "LinkedIn" };
    private SearchProviderConfig? _editingConfig = null;

    private bool CanAddProvider => !string.IsNullOrWhiteSpace(_newProviderConfig.Provider)
                                   && !string.IsNullOrWhiteSpace(_newProviderConfig.ApiKey);

    private void EditProviderConfig(SearchProviderConfig config)
    {
        _editingConfig = config;
        _newProviderConfig = new SearchProviderConfig
        {
            Provider = config.Provider,
            ApiKey = config.ApiKey,
            ApiHost = config.ApiHost
        };
    }

    private void CancelEdit()
    {
        _editingConfig = null;
        _newProviderConfig = new SearchProviderConfig { Provider = "LinkedIn" };
    }

    private async Task AddProviderConfig()
    {
        if (!CanAddProvider) return;

        if (_editingConfig != null)
        {
            _editingConfig.Provider = _newProviderConfig.Provider;
            _editingConfig.ApiKey = _newProviderConfig.ApiKey;
            _editingConfig.ApiHost = _newProviderConfig.ApiHost;
            _editingConfig = null;
        }
        else
        {
            _providerConfigs.Add(new SearchProviderConfig
            {
                Provider = _newProviderConfig.Provider,
                ApiKey = _newProviderConfig.ApiKey,
                ApiHost = _newProviderConfig.ApiHost
            });
        }

        _newProviderConfig = new SearchProviderConfig { Provider = "LinkedIn" };

        await SaveSettings();
    }

    private async Task RemoveProviderConfig(SearchProviderConfig config)
    {
        if (_editingConfig == config) CancelEdit();
        _providerConfigs.Remove(config);
        await SaveSettings();
    }

    private async Task SaveSettings()
    {
        if (_settings is null)
        {
            return;
        }

        try
        {
            _settings.SearchProviderSettingsJson = System.Text.Json.JsonSerializer.Serialize(_providerConfigs);
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
            _settings.SearchProviderSettingsJson = System.Text.Json.JsonSerializer.Serialize(_providerConfigs);
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