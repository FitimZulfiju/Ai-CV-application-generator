namespace AiCV.Web.Components.Pages;

public partial class UserSettingsPage
{
    [Inject]
    public IUserAIConfigurationService ConfigurationService { get; set; } = default!;

    [Inject]
    public IUserSettingsService UserSettingsService { get; set; } = default!; // Kept if needed for other settings, but might be removed if empty

    [Inject]
    public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    public IModelDiscoveryService DiscoveryService { get; set; } = default!;

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    private string _userId = string.Empty;
    private bool _isLoading = true;

    // List of saved configurations
    private List<UserAIConfiguration> _configurations = [];

    // Form model for adding NEW configuration
    private UserAIConfiguration _newConfig = new();

    private List<string> _availableModels = [];
    private bool _modelsLoaded = false;
    private bool _isValidating = false;
    private bool _showNewApiKey = false;
    private bool _showCostAlert = true;

    // Available providers for dropdown
    private readonly List<AIProvider> _availableProviders = [.. Enum.GetValues<AIProvider>()];

    private bool CanAddConfiguration =>
        !string.IsNullOrWhiteSpace(_newConfig.ApiKey)
        && !string.IsNullOrWhiteSpace(_newConfig.ModelId)
        && !string.IsNullOrWhiteSpace(_newConfig.Name);

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            _userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (!string.IsNullOrEmpty(_userId))
            {
                await LoadConfigurations();
            }
        }
        else
        {
            NavigationManager.NavigateTo("/login");
        }
    }

    private async Task LoadConfigurations()
    {
        _isLoading = true;
        try
        {
            _configurations = await ConfigurationService.GetConfigurationsAsync(_userId);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading configurations: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task ValidateAndLoadModels()
    {
        if (string.IsNullOrWhiteSpace(_newConfig.ApiKey))
            return;

        _isValidating = true;
        _modelsLoaded = false;
        _availableModels.Clear();

        try
        {
            // If OpenRouter, user might have entered a modelId manually in a previous step?
            // Actually, for OpenRouter we discover models.
            // For others, if key is invalid, discovery fails.

            var result = await DiscoveryService.DiscoverModelsAsync(
                _newConfig.Provider,
                _newConfig.ApiKey
            );

            if (result?.Success == true)
            {
                _availableModels = result.Models;
                _modelsLoaded = true;

                if (_availableModels.Count > 0)
                {
                    _newConfig.ModelId = _availableModels[0];
                    OnModelSelected();
                }

                Snackbar.Add($"Found {_availableModels.Count} models", Severity.Success);
            }
            else
            {
                // Fallback for providers that might not support discovery or if API is down but key is OK?
                // Actually DiscoveryService handles fallbacks internally if discovery fails but exception treated?
                // No, DiscoveryService returns Success=false if API error.
                // We should show error.
                Snackbar.Add(
                    result?.ErrorMessage ?? "Failed to validate API Key or fetch models.",
                    Severity.Error
                );

                // Optional: Allow manual entry or fallback list anyway?
                // Let's allow fallback if discovery fails, so user isn't blocked?
                // _availableModels = DiscoveryService.GetFallbackModels(_newConfig.Provider);
                // _modelsLoaded = true;
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isValidating = false;
        }
    }

    private void OnModelSelected()
    {
        // Auto-generate name if empty or if it matches a model name (meaning it was likely auto-generated)
        if (
            !string.IsNullOrWhiteSpace(_newConfig.ModelId)
            && (
                string.IsNullOrWhiteSpace(_newConfig.Name)
                || _availableModels.Contains(_newConfig.Name)
            )
        )
        {
            _newConfig.Name = _newConfig.ModelId;
        }
    }

    private Task<IEnumerable<string>> SearchModels(string value, CancellationToken _)
    {
        // If text is empty, show all models
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(_availableModels.AsEnumerable());
        }

        // Filter models by search text (case-insensitive)
        var filtered = _availableModels.Where(m =>
            m.Contains(value, StringComparison.OrdinalIgnoreCase)
        );

        return Task.FromResult(filtered);
    }

    private async Task AddConfiguration()
    {
        if (!CanAddConfiguration)
            return;

        try
        {
            _newConfig.UserId = _userId;
            // IsActive logic handled by service (first one matches)

            var saved = await ConfigurationService.SaveConfigurationAsync(_newConfig);
            if (saved != null)
            {
                // Refresh list to get correct IDs and active status
                await LoadConfigurations();
                ResetNewConfig();
                Snackbar.Add(Localizer["ConfigurationSaved"], Severity.Success);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private async Task ActivateConfiguration(UserAIConfiguration config)
    {
        try
        {
            var result = await ConfigurationService.ActivateConfigurationAsync(config.Id, _userId);
            if (result != null)
            {
                // Update local state mechanism without full reload if possible?
                // Accessing _configurations directly
                foreach (var c in _configurations)
                {
                    c.IsActive = (c.Id == config.Id);
                }

                Snackbar.Add(
                    string.Format(Localizer["ActiveConfigChanged"], config.Name),
                    Severity.Success
                );
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private async Task EditConfiguration(UserAIConfiguration config)
    {
        // Clone config for editing to prevent modifying the list directly before saving
        var configToEdit = new UserAIConfiguration
        {
            Id = config.Id,
            UserId = config.UserId,
            Provider = config.Provider,
            Name = config.Name,
            ApiKey = config.ApiKey,
            ModelId = config.ModelId,
            IsActive = config.IsActive,
            CreatedAt = config.CreatedAt,
        };

        // We need to fetch models for this config to populate dropdown in dialog
        // Fallback first to avoid delay
        var models = DiscoveryService.GetFallbackModels(config.Provider);

        // If we want real discovery on edit, we might need a "Refresh Models" button in dialog
        // or try to discover immediately if we have the key (which we do, unprotectd).
        // Let's try discovery if key is present.
        if (string.IsNullOrEmpty(config.ApiKey))
        {
            Snackbar.Add("Warning: API Key is missing. Please enter it.", Severity.Warning);
        }
        else if (config.ApiKey == "DECRYPTION_FAILED")
        {
            Snackbar.Add(
                "Error: API Key could not be decrypted. This usually happens after a system restart if keys weren't persisted, or if keys were rotated. Please re-enter it.",
                Severity.Error
            );
            configToEdit.ApiKey = string.Empty; // Clear the token so user doesn't save it
        }
        else
        {
            // Try discovery if we have a valid-looking key
            try
            {
                var discoveryResult = await DiscoveryService.DiscoverModelsAsync(
                    config.Provider,
                    config.ApiKey
                );
                if (discoveryResult.Success && discoveryResult.Models.Count > 0)
                {
                    models = discoveryResult.Models;
                }
            }
            catch
            { /* ignore, stick to fallback */
            }
        }

        var parameters = new DialogParameters<AISettingsEditDialog>
        {
            { x => x.Configuration, configToEdit },
            { x => x.AvailableModels, models },
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        var dialog = await DialogService.ShowAsync<AISettingsEditDialog>(
            Localizer["EditConfiguration"],
            parameters,
            options
        );
        var result = await dialog.Result;

        if (result?.Canceled == false && result.Data is UserAIConfiguration updatedConfig)
        {
            try
            {
                // ensure UserID is set (should be)
                updatedConfig.UserId = _userId;
                await ConfigurationService.SaveConfigurationAsync(updatedConfig);

                // Refresh list
                await LoadConfigurations();
                Snackbar.Add(Localizer["ConfigurationUpdated"], Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error updating: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task DeleteConfiguration(UserAIConfiguration config)
    {
        var confirmed = await DialogService.ShowMessageBox(
            Localizer["DeleteConfiguration"],
            Localizer["DeleteConfigConfirmation"],
            yesText: Localizer["Delete"],
            cancelText: Localizer["Cancel"]
        );

        if (confirmed == true)
        {
            try
            {
                var success = await ConfigurationService.DeleteConfigurationAsync(
                    config.Id,
                    _userId
                );
                if (success)
                {
                    _configurations.Remove(config);
                    // If active was deleted, service might have activated another. Reload to be safe.
                    await LoadConfigurations();
                    Snackbar.Add(Localizer["ConfigurationDeleted"], Severity.Success);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error deleting: {ex.Message}", Severity.Error);
            }
        }
    }

    private void ResetNewConfig()
    {
        _newConfig = new UserAIConfiguration { Provider = AIProvider.OpenAI }; // Default provider
        _availableModels.Clear();
        _modelsLoaded = false;
        _showNewApiKey = false;
    }

    private static Color GetProviderColor(AIProvider provider) =>
        provider switch
        {
            AIProvider.GoogleGemini => Color.Primary,
            AIProvider.OpenAI => Color.Success,
            AIProvider.Claude => Color.Warning,
            AIProvider.Groq => Color.Info,
            AIProvider.DeepSeek => Color.Secondary,
            _ => Color.Default,
        };

    private static string GetProviderIcon(AIProvider provider) =>
        provider switch
        {
            AIProvider.GoogleGemini => Icons.Material.Filled.AutoAwesome,
            AIProvider.OpenAI => Icons.Material.Filled.Psychology,
            AIProvider.Claude => Icons.Material.Filled.SmartToy,
            AIProvider.Groq => Icons.Material.Filled.Speed,
            AIProvider.DeepSeek => Icons.Material.Filled.Explore,
            _ => Icons.Material.Filled.Memory,
        };
}
