namespace AiCV.Web.Components.Pages;

public partial class UserSettingsPage
{
    private string _userId = string.Empty;
    private bool _isLoading = true;

    // List of saved configurations
    private List<UserAIConfiguration> _configurations = [];

    // Form model for adding NEW configuration
    private UserAIConfiguration _newConfig = new();

    private List<AIModelDto> _availableModels = [];
    private AIModelDto? _selectedModelMetadata;
    private bool _modelsLoaded = false;
    private bool _isValidating = false;
    private bool _isTestingConnection = false;
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
            Navigation.NavigateTo($"/{NavUri.LoginPage}");
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
        _selectedModelMetadata = null;

        try
        {
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
                    var firstModel = _availableModels[0];
                    _newConfig.ModelId = firstModel.ModelId;
                    _selectedModelMetadata = firstModel;
                    OnModelSelected();
                }

                Snackbar.Add($"Found {_availableModels.Count} models", Severity.Success);
            }
            else
            {
                Snackbar.Add(
                    result?.ErrorMessage ?? "Failed to validate API Key or fetch models.",
                    Severity.Error
                );
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
        if (string.IsNullOrWhiteSpace(_newConfig.ModelId))
        {
            _selectedModelMetadata = null;
            return;
        }

        _selectedModelMetadata = _availableModels.FirstOrDefault(m =>
            m.ModelId == _newConfig.ModelId
        );

        if (_selectedModelMetadata != null)
        {
            _newConfig.CostType = _selectedModelMetadata.CostType;
            _newConfig.Notes = _selectedModelMetadata.Notes.Count != 0
                ? string.Join(", ", _selectedModelMetadata.Notes)
                : null;
        }

        // Auto-generate name if empty or if it matches a model name (meaning it was likely auto-generated)
        if (
            !string.IsNullOrWhiteSpace(_newConfig.ModelId)
            && (
                string.IsNullOrWhiteSpace(_newConfig.Name)
                || _availableModels.Any(m => m.ModelId == _newConfig.Name)
            )
        )
        {
            _newConfig.Name = _newConfig.ModelId;
        }
    }

    private async Task TestConnection()
    {
        if (
            string.IsNullOrWhiteSpace(_newConfig.ApiKey)
            || string.IsNullOrWhiteSpace(_newConfig.ModelId)
        )
        {
            return;
        }

        _isTestingConnection = true;
        try
        {
            // We use the factory to get a temporary service instance
            var aiService = AIServiceFactory.CreateService(
                _newConfig.Provider,
                _newConfig.ApiKey,
                _newConfig.ModelId,
                Localizer,
                new HttpClient() // Temporary HttpClient
            );

            var result = await aiService.TestAccessAsync();

            if (result.Success)
            {
                Snackbar.Add(Localizer["AccessCheckSuccess"], Severity.Success);
            }
            else
            {
                Snackbar.Add(
                    $"{Localizer["AccessCheckFailed"]}: {result.Message}",
                    Severity.Warning
                );
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Test failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isTestingConnection = false;
        }
    }

    private Task<IEnumerable<string>> SearchModels(string value, CancellationToken _)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(_availableModels.Select(m => m.ModelId).AsEnumerable());
        }

        var filtered = _availableModels
            .Where(m =>
                m.ModelId.Contains(value, StringComparison.OrdinalIgnoreCase)
                || (m.Name?.Contains(value, StringComparison.OrdinalIgnoreCase) == true)
            )
            .Select(m => m.ModelId);

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
            CostType = config.CostType,
            Notes = config.Notes,
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
                else if (!discoveryResult.Success)
                {
                    Snackbar.Add(
                        $"{Localizer["DiscoveryFailed"]}: {discoveryResult.ErrorMessage}",
                        Severity.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Discovery error: {ex.Message}", Severity.Warning);
            }
        }

        var parameters = new DialogParameters<AISettingsEditDialog>
        {
            { x => x.UserConfiguration, configToEdit },
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
