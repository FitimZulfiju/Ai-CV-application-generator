namespace AiCV.Web.Components.Pages;

public partial class UserSettingsPage
{
    private string _userId = string.Empty;
    private string _userEmail = string.Empty;
    private bool _isLoading = true;
    private bool _isProtected = false;
    private int _activeSettingsTabIndex;

    // List of saved configurations
    private List<UserAIConfiguration> _configurations = [];

    // Form model for adding NEW configuration
    private UserAIConfiguration _newConfig = new();

    private List<AIModelDto> _availableModels = [];
    private AIModelDto? _selectedModelMetadata;
    private bool _modelsLoaded;
    private bool _isValidating;
    private bool _isTestingConnection;
    private bool _showNewApiKey;
    private bool _showCostAlert = true;
    private bool _isDeleted;
    private bool _hasPassword;
    private bool _isSettingPassword;
    private bool _isChangingPassword;
    private bool _showSetPassword;
    private bool _showChangePassword;
    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmNewPassword = string.Empty;

    // Available providers for dropdown
    private readonly List<AIProvider> _availableProviders = [.. Enum.GetValues<AIProvider>()];

    private bool CanAddConfiguration =>
        !string.IsNullOrWhiteSpace(_newConfig.ApiKey)
        && !string.IsNullOrWhiteSpace(_newConfig.ModelId)
        && !string.IsNullOrWhiteSpace(_newConfig.Name);

    private void ConnectOpenRouter() =>
        Navigation.NavigateTo("/connect/openrouter", forceLoad: true);

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            _userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            // Try to get email from claims first, then fallback to Identity.Name
            _userEmail =
                user.FindFirst(ClaimTypes.Email)?.Value ?? user.Identity?.Name ?? string.Empty;

            // Mark as protected if it's the default demo account
            _isProtected = string.Equals(
                _userEmail,
                "demouser@aicv.com",
                StringComparison.OrdinalIgnoreCase
            );

            if (!string.IsNullOrEmpty(_userId))
            {
                await LoadAccountSecurity();
                await LoadConfigurations();
                HandleOAuthReturn();
            }
        }
        else
        {
            Navigation.NavigateTo($"/{NavUri.LoginPage}");
        }
    }

    private void HandleOAuthReturn()
    {
        var uri = new Uri(Navigation.Uri);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("connected", out var connected))
        {
            var msg = connected.ToString() switch
            {
                "openrouter" => "OpenRouter account connected! You can now use it from the Generate page.",
                "gemini" => "Google Gemini account connected! Your existing plan will be used.",
                _ => "Account connected successfully!",
            };
            Snackbar.Add(msg, Severity.Success);
            Navigation.NavigateTo($"/{NavUri.SettingsPage}", replace: true);
        }
        else if (query.TryGetValue("error", out var errorCode))
        {
            var msg = errorCode.ToString() switch
            {
                "openrouter_cancelled" => "OpenRouter connection was cancelled.",
                "openrouter_expired" => "OpenRouter session expired. Please try again.",
                "openrouter_exchange_failed" => "Could not retrieve OpenRouter API key.",
                "gemini_cancelled" => "Google Gemini connection was cancelled.",
                "gemini_expired" => "Session expired. Please try again.",
                "gemini_exchange_failed" => "Could not exchange code with Google. Please try again.",
                "gemini_no_refresh_token" => "Google did not return a refresh token. Please revoke the app access in your Google account and try again.",
                "google_not_configured" => "Google OAuth is not configured on this server.",
                _ => $"Connection failed ({errorCode}).",
            };

            // Append raw provider error detail if present
            if (query.TryGetValue("detail", out var detail) && !string.IsNullOrWhiteSpace(detail))
                msg += $" Details: {detail}";

            Snackbar.Add(msg, Severity.Error);
            Navigation.NavigateTo($"/{NavUri.SettingsPage}", replace: true);
        }

    }

    private async Task LoadAccountSecurity()
    {
        var user = await UserManager.FindByIdAsync(_userId);
        if (user == null)
            return;

        _hasPassword = await UserManager.HasPasswordAsync(user);
    }

    private async Task SetLocalPassword()
    {
        if (_isSettingPassword)
            return;

        if (_newPassword != _confirmNewPassword)
        {
            Snackbar.Add(Localizer["PasswordsDoNotMatch"], Severity.Error);
            return;
        }

        _isSettingPassword = true;
        try
        {
            var user = await UserManager.FindByIdAsync(_userId);
            if (user == null)
            {
                Snackbar.Add(Localizer["AccountNotFoundSignInAgain"], Severity.Error);
                return;
            }

            if (await UserManager.HasPasswordAsync(user))
            {
                _hasPassword = true;
                _showSetPassword = false;
                Snackbar.Add(Localizer["AccountAlreadyHasPassword"], Severity.Info);
                return;
            }

            var result = await UserManager.AddPasswordAsync(user, _newPassword);
            if (result.Succeeded)
            {
                _hasPassword = true;
                _showSetPassword = false;
                ClearPasswordFields();
                Snackbar.Add(Localizer["PasswordAddedLocalLoginEnabled"], Severity.Success);
            }
            else
            {
                Snackbar.Add(string.Join(" ", result.Errors.Select(e => e.Description)), Severity.Error);
            }
        }
        finally
        {
            _isSettingPassword = false;
        }
    }

    private async Task ChangeLocalPassword()
    {
        if (_isChangingPassword)
            return;

        if (_newPassword != _confirmNewPassword)
        {
            Snackbar.Add(Localizer["PasswordsDoNotMatch"], Severity.Error);
            return;
        }

        _isChangingPassword = true;
        try
        {
            var user = await UserManager.FindByIdAsync(_userId);
            if (user == null)
            {
                Snackbar.Add(Localizer["AccountNotFoundSignInAgain"], Severity.Error);
                return;
            }

            if (!await UserManager.HasPasswordAsync(user))
            {
                _hasPassword = false;
                _showChangePassword = false;
                Snackbar.Add(Localizer["AccountHasNoPasswordYet"], Severity.Info);
                return;
            }

            var result = await UserManager.ChangePasswordAsync(user, _currentPassword, _newPassword);
            if (result.Succeeded)
            {
                await SignInManager.RefreshSignInAsync(user);
                _showChangePassword = false;
                ClearPasswordFields();
                Snackbar.Add(Localizer["PasswordChangedSuccessfully"], Severity.Success);
            }
            else
            {
                Snackbar.Add(string.Join(" ", result.Errors.Select(e => e.Description)), Severity.Error);
            }
        }
        finally
        {
            _isChangingPassword = false;
        }
    }

    private void HidePasswordForms()
    {
        _showSetPassword = false;
        _showChangePassword = false;
        ClearPasswordFields();
    }

    private void ClearPasswordFields()
    {
        _currentPassword = string.Empty;
        _newPassword = string.Empty;
        _confirmNewPassword = string.Empty;
    }

    private async Task LoadConfigurations()
    {
        _isLoading = true;
        LoadingService.Show("Loading settings...", 0);
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
            LoadingService.Hide();
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
            _newConfig.Notes =
                _selectedModelMetadata.Notes.Count != 0
                    ? string.Join(", ", _selectedModelMetadata.Notes)
                    : null;

            if (
                string.IsNullOrWhiteSpace(_newConfig.Name)
                || _availableModels.Any(m => m.ModelId == _newConfig.Name)
            )
            {
                _newConfig.Name = _newConfig.ModelId;
            }
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
            var aiService = AIServiceFactory.CreateService(
                _newConfig.Provider,
                _newConfig.ApiKey,
                _newConfig.ModelId,
                Localizer,
                new HttpClient()
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

    /// <summary>Adapter called by AiModelPicker after selection — syncs metadata to _newConfig.</summary>
    private Task OnModelPickerChanged(string? modelId)
    {
        _newConfig.ModelId = modelId ?? string.Empty;
        OnModelSelected();
        return Task.CompletedTask;
    }

    private async Task AddConfiguration()
    {
        if (!CanAddConfiguration)
            return;

        try
        {
            _newConfig.UserId = _userId;

            var saved = await ConfigurationService.SaveConfigurationAsync(_newConfig);
            if (saved != null)
            {
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

        var models = DiscoveryService.GetFallbackModels(config.Provider);

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
            configToEdit.ApiKey = string.Empty;
        }
        else
        {
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
                updatedConfig.UserId = _userId;
                await ConfigurationService.SaveConfigurationAsync(updatedConfig);
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
        var confirmed = await DialogService.ShowMessageBoxAsync(
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
        _newConfig = new UserAIConfiguration { Provider = AIProvider.OpenAI };
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

    private async Task HandleDeleteAccount()
    {
        if (_isProtected)
            return;

        var confirmed = await DialogService.ShowMessageBoxAsync(
            Localizer["DeleteAccount"],
            Localizer["DeleteAccountWarning"],
            yesText: Localizer["DeleteMyAccountPermanently"],
            cancelText: Localizer["Cancel"]
        );

        if (confirmed == true)
        {
            _isLoading = true;
            LoadingService.Show("Deleting account...", 0);
            StateHasChanged();

            try
            {
                var success = await UserManagementService.DeleteUserAccountAsync(_userId);
                if (success)
                {
                    _isDeleted = true;
                    Snackbar.Add(Localizer["AccountDeletedSuccess"], Severity.Success);
                }
                else
                {
                    Snackbar.Add(Localizer["AccountDeleteFailed"], Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"{Localizer["Error"]}: {ex.Message}", Severity.Error);
            }
            finally
            {
                _isLoading = false;
                LoadingService.Hide();
                StateHasChanged();
            }
        }
    }

    private void HandleFinalLogout()
    {
        Navigation.NavigateTo("/logout-direct", forceLoad: true);
    }

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
