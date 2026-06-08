namespace AiCV.Web.Components.Pages;

public partial class UserSettingsPage
{
    private const long MaxBackupImportFileBytes = 1024 * 1024 * 15;
    private const int MaxBackupTextLength = 100_000;
    private const int MaxBackupCollectionItems = 2_000;
    private const int MaxProfileTextLength = 20_000;
    private const int MaxProfileCollectionItems = 500;

    private static readonly JsonSerializerOptions BackupJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

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
    private bool _isBackupBusy;
    private bool _backupProfile = true;
    private bool _backupApplications = true;
    private bool _backupSettings = true;
    private bool _backupNotes = true;

    // Available providers for dropdown
    private readonly List<AIProvider> _availableProviders = [.. Enum.GetValues<AIProvider>()];

    private bool HasSelectedBackupSections =>
        _backupProfile || _backupApplications || _backupSettings || _backupNotes;

    private bool AllBackupSectionsSelected
    {
        get => _backupProfile && _backupApplications && _backupSettings && _backupNotes;
        set
        {
            _backupProfile = value;
            _backupApplications = value;
            _backupSettings = value;
            _backupNotes = value;
        }
    }

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
                AiCV.Application.Common.DemoConstants.DemoUserEmail,
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

    private async Task ExportSelectedBackup()
    {
        if (!HasSelectedBackupSections || string.IsNullOrEmpty(_userId))
        {
            Snackbar.Add(Localizer["SelectAtLeastOneBackupSection"], Severity.Warning);
            return;
        }

        _isBackupBusy = true;
        try
        {
            var backup = new UserDataBackup
            {
                ExportedAtUtc = DateTime.UtcNow,
                Sections = new BackupSections
                {
                    Profile = _backupProfile,
                    Applications = _backupApplications,
                    Settings = _backupSettings,
                    Notes = _backupNotes,
                },
            };

            if (_backupProfile)
            {
                var profile = await CVService.GetProfileAsync(_userId);
                backup.Profile = profile is null ? null : CloneProfileForExport(profile);
            }

            await using var context = await DbContextFactory.CreateDbContextAsync();

            if (_backupApplications)
            {
                var applications = await context
                    .GeneratedApplications.AsNoTracking()
                    .Include(a => a.JobPosting)
                    .Where(a => a.UserId == _userId)
                    .OrderByDescending(a => a.CreatedDate)
                    .ToListAsync();

                backup.Applications =
                [
                    .. applications.Select(a => new ApplicationBackup
                    {
                        JobPosting = a.JobPosting is null
                            ? new JobPostingBackup()
                            : new JobPostingBackup
                            {
                                Title = a.JobPosting.Title,
                                CompanyName = a.JobPosting.CompanyName,
                                Description = a.JobPosting.Description,
                                Url = a.JobPosting.Url,
                                DatePosted = a.JobPosting.DatePosted,
                            },
                        CoverLetterContent = a.CoverLetterContent,
                        TailoredResumeJson = a.TailoredResumeJson,
                        ApplicationEmailContent = a.ApplicationEmailContent,
                        Template = a.Template,
                        CreatedDate = a.CreatedDate,
                    }),
                ];
            }

            if (_backupSettings)
            {
                var settings = await UserSettingsService.GetUserSettingsAsync(_userId);
                if (settings is not null)
                {
                    backup.Settings = new UserSettingsBackup
                    {
                        OpenAIApiKey = settings.OpenAIApiKey,
                        GoogleGeminiApiKey = settings.GoogleGeminiApiKey,
                        ClaudeApiKey = settings.ClaudeApiKey,
                        GroqApiKey = settings.GroqApiKey,
                        DeepSeekApiKey = settings.DeepSeekApiKey,
                        OpenRouterApiKey = settings.OpenRouterApiKey,
                        DefaultProvider = settings.DefaultProvider,
                        DefaultModelId = settings.DefaultModelId,
                    };
                }

                var configurations = await ConfigurationService.GetConfigurationsAsync(_userId);
                backup.AIConfigurations =
                [
                    .. configurations.Select(c => new UserAIConfigurationBackup
                    {
                        Provider = c.Provider,
                        Name = c.Name,
                        ApiKey = c.ApiKey,
                        ModelId = c.ModelId,
                        CostType = c.CostType,
                        Notes = c.Notes,
                        IsActive = c.IsActive,
                        CreatedAt = c.CreatedAt,
                    }),
                ];
            }

            if (_backupNotes)
            {
                var notes = await context
                    .Notes.AsNoTracking()
                    .Where(n => n.UserId == _userId)
                    .OrderByDescending(n => n.IsPinned)
                    .ThenBy(n => n.DisplayOrder)
                    .ThenByDescending(n => n.UpdatedAt)
                    .ToListAsync();

                backup.Notes =
                [
                    .. notes.Select(n => new NoteBackup
                    {
                        Title = n.Title,
                        Content = n.Content,
                        Color = n.Color,
                        IsPinned = n.IsPinned,
                        IsArchived = n.IsArchived,
                        DisplayOrder = n.DisplayOrder,
                        CreatedAt = n.CreatedAt,
                        UpdatedAt = n.UpdatedAt,
                    }),
                ];
            }

            var json = JsonSerializer.Serialize(backup, BackupJsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            var fileName = $"aicv-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";

            await using var stream = new MemoryStream(bytes);
            using var streamReference = new DotNetStreamReference(stream);
            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamReference);

            Snackbar.Add(Localizer["BackupExported"], Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"{Localizer["BackupExportFailed"]}: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isBackupBusy = false;
        }
    }

    private async Task ImportSelectedBackup(InputFileChangeEventArgs fileChange)
    {
        if (!HasSelectedBackupSections || string.IsNullOrEmpty(_userId) || fileChange.File is null)
        {
            Snackbar.Add(Localizer["SelectAtLeastOneBackupSection"], Severity.Warning);
            return;
        }

        var confirmed = await DialogService.ShowMessageBoxAsync(
            Localizer["ImportBackupWarningTitle"],
            Localizer["ImportBackupWarningContent"],
            yesText: Localizer["ImportBackup"],
            cancelText: Localizer["Cancel"]
        );

        if (confirmed != true)
        {
            return;
        }

        _isBackupBusy = true;
        try
        {
            var file = fileChange.File;
            if (!IsBackupJsonFile(file))
            {
                Snackbar.Add(Localizer["InvalidBackupJson"], Severity.Error);
                return;
            }

            await using var stream = file.OpenReadStream(maxAllowedSize: MaxBackupImportFileBytes);
            var backup = await JsonSerializer.DeserializeAsync<UserDataBackup>(
                stream,
                BackupJsonOptions
            );

            if (backup is null || !IsValidBackup(backup))
            {
                Snackbar.Add(Localizer["InvalidBackupJson"], Severity.Error);
                return;
            }

            if (!BackupContainsSelectedSections(backup))
            {
                var missingConfirmed = await DialogService.ShowMessageBoxAsync(
                    Localizer["BackupMissingSectionsWarningTitle"],
                    Localizer["BackupMissingSectionsWarningContent"],
                    yesText: Localizer["Continue"],
                    cancelText: Localizer["Cancel"]
                );

                if (missingConfirmed != true)
                {
                    return;
                }
            }

            if (_backupProfile && backup.Profile is not null && backup.Sections.Profile)
            {
                await ImportProfileBackup(backup.Profile);
            }

            await using var context = await DbContextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            if (_backupApplications && backup.Sections.Applications)
            {
                await ReplaceApplications(context, backup.Applications);
            }

            if (_backupNotes && backup.Sections.Notes)
            {
                await ReplaceNotes(context, backup.Notes);
            }

            await transaction.CommitAsync();

            if (_backupSettings && backup.Sections.Settings)
            {
                await ReplaceSettings(backup);
                await LoadConfigurations();
            }

            Snackbar.Add(Localizer["BackupImported"], Severity.Success);
        }
        catch (JsonException)
        {
            Snackbar.Add(Localizer["InvalidBackupJson"], Severity.Error);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"{Localizer["BackupImportFailed"]}: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isBackupBusy = false;
        }
    }

    private static bool IsBackupJsonFile(IBrowserFile file)
    {
        return file.Size is > 0 and <= MaxBackupImportFileBytes
            && (
                file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(file.ContentType, "application/json", StringComparison.OrdinalIgnoreCase)
            );
    }

    private bool BackupContainsSelectedSections(UserDataBackup backup)
    {
        return (!_backupProfile || (backup.Sections.Profile && backup.Profile is not null))
            && (!_backupApplications || backup.Sections.Applications)
            && (!_backupSettings || backup.Sections.Settings)
            && (!_backupNotes || backup.Sections.Notes);
    }

    private static bool IsValidBackup(UserDataBackup backup)
    {
        return backup.Version == 1
            && backup.Applications.Count <= MaxBackupCollectionItems
            && backup.AIConfigurations.Count <= MaxBackupCollectionItems
            && backup.Notes.Count <= MaxBackupCollectionItems
            && (backup.Profile is null || IsValidImportedProfile(backup.Profile))
            && backup.Applications.All(IsValidApplicationBackup)
            && (backup.Settings is null || IsValidSettingsBackup(backup.Settings))
            && backup.AIConfigurations.All(IsValidAIConfigurationBackup)
            && backup.Notes.All(IsValidNoteBackup);
    }

    private async Task ImportProfileBackup(CandidateProfile importedProfile)
    {
        var currentProfile = await CVService.GetProfileAsync(_userId);
        if (currentProfile is null)
        {
            throw new InvalidOperationException("Current profile not found.");
        }

        NormalizeImportedProfile(importedProfile, currentProfile);
        await CVService.SaveProfileAsync(importedProfile);
    }

    private async Task ReplaceApplications(ApplicationDbContext context, List<ApplicationBackup> applications)
    {
        var profile = await context.CandidateProfiles.AsNoTracking().FirstOrDefaultAsync(p =>
            p.UserId == _userId
        );

        if (profile is null)
        {
            throw new InvalidOperationException("Current profile not found.");
        }

        var existingApplications = await context
            .GeneratedApplications.Where(a => a.UserId == _userId)
            .ToListAsync();
        var oldJobPostingIds = existingApplications.Select(a => a.JobPostingId).Distinct().ToList();

        context.GeneratedApplications.RemoveRange(existingApplications);
        await context.SaveChangesAsync();

        var orphanedJobPostings = await context
            .JobPostings.Where(j =>
                oldJobPostingIds.Contains(j.Id)
                && !context.GeneratedApplications.Any(a => a.JobPostingId == j.Id)
            )
            .ToListAsync();
        context.JobPostings.RemoveRange(orphanedJobPostings);

        foreach (var application in applications)
        {
            var jobPosting = new JobPosting
            {
                Title = application.JobPosting.Title ?? string.Empty,
                CompanyName = application.JobPosting.CompanyName ?? string.Empty,
                Description = application.JobPosting.Description ?? string.Empty,
                Url = application.JobPosting.Url ?? string.Empty,
                DatePosted = application.JobPosting.DatePosted,
            };

            context.JobPostings.Add(jobPosting);
            context.GeneratedApplications.Add(
                new GeneratedApplication
                {
                    UserId = _userId,
                    JobPosting = jobPosting,
                    CandidateProfileId = profile.Id,
                    CoverLetterContent = application.CoverLetterContent ?? string.Empty,
                    TailoredResumeJson = application.TailoredResumeJson ?? string.Empty,
                    ApplicationEmailContent = application.ApplicationEmailContent ?? string.Empty,
                    Template = application.Template,
                    CreatedDate = application.CreatedDate,
                }
            );
        }

        await context.SaveChangesAsync();
    }

    private async Task ReplaceNotes(ApplicationDbContext context, List<NoteBackup> notes)
    {
        var existingNotes = await context.Notes.Where(n => n.UserId == _userId).ToListAsync();
        context.Notes.RemoveRange(existingNotes);

        foreach (var note in notes)
        {
            context.Notes.Add(
                new Note
                {
                    UserId = _userId,
                    Title = note.Title,
                    Content = note.Content,
                    Color = string.IsNullOrWhiteSpace(note.Color) ? "default" : note.Color,
                    IsPinned = note.IsPinned,
                    IsArchived = note.IsArchived,
                    DisplayOrder = note.DisplayOrder,
                    CreatedAt = note.CreatedAt,
                    UpdatedAt = note.UpdatedAt,
                }
            );
        }

        await context.SaveChangesAsync();
    }

    private async Task ReplaceSettings(UserDataBackup backup)
    {
        if (backup.Settings is not null)
        {
            await UserSettingsService.SaveUserSettingsAsync(
                _userId,
                backup.Settings.OpenAIApiKey,
                backup.Settings.GoogleGeminiApiKey,
                backup.Settings.ClaudeApiKey,
                backup.Settings.GroqApiKey,
                backup.Settings.DeepSeekApiKey,
                backup.Settings.OpenRouterApiKey,
                backup.Settings.DefaultProvider,
                backup.Settings.DefaultModelId
            );
        }

        await using var context = await DbContextFactory.CreateDbContextAsync();
        var existingConfigurations = await context
            .UserAIConfigurations.Where(c => c.UserId == _userId)
            .ToListAsync();
        context.UserAIConfigurations.RemoveRange(existingConfigurations);
        await context.SaveChangesAsync();

        foreach (var configuration in backup.AIConfigurations)
        {
            await ConfigurationService.SaveConfigurationAsync(
                new UserAIConfiguration
                {
                    UserId = _userId,
                    Provider = configuration.Provider,
                    Name = configuration.Name ?? string.Empty,
                    ApiKey = configuration.ApiKey,
                    ModelId = configuration.ModelId,
                    CostType = configuration.CostType,
                    Notes = configuration.Notes,
                    IsActive = configuration.IsActive,
                    CreatedAt = configuration.CreatedAt,
                }
            );
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

    private static bool IsValidApplicationBackup(ApplicationBackup application)
    {
        return application.JobPosting is not null
            && IsValidBackupText(application.JobPosting.Title)
            && IsValidBackupText(application.JobPosting.CompanyName)
            && IsValidBackupText(application.JobPosting.Description)
            && IsValidBackupText(application.JobPosting.Url)
            && IsValidBackupText(application.CoverLetterContent)
            && IsValidBackupText(application.TailoredResumeJson)
            && IsValidBackupText(application.ApplicationEmailContent);
    }

    private static bool IsValidSettingsBackup(UserSettingsBackup settings)
    {
        return IsValidBackupText(settings.OpenAIApiKey)
            && IsValidBackupText(settings.GoogleGeminiApiKey)
            && IsValidBackupText(settings.ClaudeApiKey)
            && IsValidBackupText(settings.GroqApiKey)
            && IsValidBackupText(settings.DeepSeekApiKey)
            && IsValidBackupText(settings.OpenRouterApiKey)
            && IsValidBackupText(settings.DefaultModelId);
    }

    private static bool IsValidAIConfigurationBackup(UserAIConfigurationBackup configuration)
    {
        return IsValidBackupText(configuration.Name)
            && IsValidBackupText(configuration.ApiKey)
            && IsValidBackupText(configuration.ModelId)
            && IsValidBackupText(configuration.CostType)
            && IsValidBackupText(configuration.Notes);
    }

    private static bool IsValidNoteBackup(NoteBackup note)
    {
        return IsValidBackupText(note.Title)
            && IsValidBackupText(note.Content)
            && IsValidBackupText(note.Color);
    }

    private static bool IsValidBackupText(string? value)
    {
        return value == null || value.Length <= MaxBackupTextLength;
    }

    private static bool IsValidImportedProfile(CandidateProfile profile)
    {
        EnsureProfileCollections(profile);

        return HasValidProfileTextLengths(profile)
            && profile.WorkExperience.Count <= MaxProfileCollectionItems
            && profile.Educations.Count <= MaxProfileCollectionItems
            && profile.Skills.Count <= MaxProfileCollectionItems
            && profile.Projects.Count <= MaxProfileCollectionItems
            && profile.Languages.Count <= MaxProfileCollectionItems
            && profile.Interests.Count <= MaxProfileCollectionItems
            && profile.WorkExperience.All(HasValidExperienceTextLengths)
            && profile.Educations.All(HasValidEducationTextLengths)
            && profile.Skills.All(HasValidSkillTextLengths)
            && profile.Projects.All(HasValidProjectTextLengths)
            && profile.Languages.All(HasValidLanguageTextLengths)
            && profile.Interests.All(HasValidInterestTextLengths);
    }

    private static bool HasValidProfileTextLengths(CandidateProfile profile)
    {
        return IsValidProfileText(profile.UserId)
            && IsValidProfileText(profile.FullName)
            && IsValidProfileText(profile.Title)
            && IsValidProfileText(profile.Email)
            && IsValidProfileText(profile.PhoneNumber)
            && IsValidProfileText(profile.LinkedInUrl)
            && IsValidProfileText(profile.PortfolioUrl)
            && IsValidProfileText(profile.Location)
            && IsValidProfileText(profile.ProfessionalSummary)
            && IsValidProfileText(profile.ProfilePictureUrl)
            && IsValidProfileText(profile.Tagline)
            && IsValidSectionConfig(profile.SummarySection)
            && IsValidSectionConfig(profile.ExperienceSection)
            && IsValidSectionConfig(profile.EducationSection)
            && IsValidSectionConfig(profile.SkillsSection)
            && IsValidSectionConfig(profile.ProjectsSection)
            && IsValidSectionConfig(profile.LanguagesSection)
            && IsValidSectionConfig(profile.InterestsSection);
    }

    private static bool IsValidSectionConfig(AiCV.Domain.SectionConfig? config)
    {
        if (config == null) return true;
        return IsValidProfileText(config.Title) && IsValidProfileText(config.Icon);
    }

    private static bool HasValidExperienceTextLengths(Experience experience)
    {
        return IsValidProfileText(experience.CompanyName)
            && IsValidProfileText(experience.JobTitle)
            && IsValidProfileText(experience.Description)
            && IsValidProfileText(experience.Location);
    }

    private static bool HasValidEducationTextLengths(Education education)
    {
        return IsValidProfileText(education.InstitutionName)
            && IsValidProfileText(education.Degree)
            && IsValidProfileText(education.Description);
    }

    private static bool HasValidSkillTextLengths(Skill skill)
    {
        return IsValidProfileText(skill.Name) && IsValidProfileText(skill.Category);
    }

    private static bool HasValidProjectTextLengths(Project project)
    {
        return IsValidProfileText(project.Name)
            && IsValidProfileText(project.Role)
            && IsValidProfileText(project.Description)
            && IsValidProfileText(project.SectionTitle)
            && IsValidProfileText(project.SectionDescription)
            && IsValidProfileText(project.Technologies)
            && IsValidProfileText(project.Link);
    }

    private static bool HasValidLanguageTextLengths(Language language)
    {
        return IsValidProfileText(language.Name) && IsValidProfileText(language.Proficiency);
    }

    private static bool HasValidInterestTextLengths(Interest interest)
    {
        return IsValidProfileText(interest.Name);
    }

    private static bool IsValidProfileText(string? value)
    {
        return value == null || value.Length <= MaxProfileTextLength;
    }

    private static CandidateProfile CloneProfileForExport(CandidateProfile profile)
    {
        var json = JsonSerializer.Serialize(profile, BackupJsonOptions);
        var exportProfile = JsonSerializer.Deserialize<CandidateProfile>(json, BackupJsonOptions)
            ?? new CandidateProfile();

        exportProfile.Id = 0;
        exportProfile.UserId = string.Empty;
        exportProfile.User = null;

        foreach (var skill in exportProfile.Skills)
        {
            skill.Id = 0;
            skill.CandidateProfileId = 0;
            skill.CandidateProfile = null;
        }

        foreach (var experience in exportProfile.WorkExperience)
        {
            experience.Id = 0;
            experience.CandidateProfileId = 0;
            experience.CandidateProfile = null;
        }

        foreach (var education in exportProfile.Educations)
        {
            education.Id = 0;
            education.CandidateProfileId = 0;
            education.CandidateProfile = null;
        }

        foreach (var project in exportProfile.Projects)
        {
            project.Id = 0;
            project.CandidateProfileId = 0;
            project.CandidateProfile = null;
        }

        foreach (var language in exportProfile.Languages)
        {
            language.Id = 0;
            language.CandidateProfileId = 0;
            language.CandidateProfile = null;
        }

        foreach (var interest in exportProfile.Interests)
        {
            interest.Id = 0;
            interest.CandidateProfileId = 0;
            interest.CandidateProfile = null;
        }

        return exportProfile;
    }

    private static void NormalizeImportedProfile(CandidateProfile importedProfile, CandidateProfile currentProfile)
    {
        EnsureProfileCollections(importedProfile);

        importedProfile.Id = currentProfile.Id;
        importedProfile.UserId = currentProfile.UserId;
        importedProfile.User = null;
        importedProfile.FullName ??= string.Empty;
        importedProfile.Title ??= string.Empty;
        importedProfile.Email ??= string.Empty;
        importedProfile.PhoneNumber ??= string.Empty;
        importedProfile.LinkedInUrl ??= string.Empty;
        importedProfile.PortfolioUrl ??= string.Empty;
        importedProfile.Location ??= string.Empty;
        importedProfile.ProfessionalSummary ??= string.Empty;
        importedProfile.ProfilePictureUrl ??= string.Empty;
        importedProfile.Tagline ??= string.Empty;

        importedProfile.SummarySection ??= new();
        importedProfile.ExperienceSection ??= new();
        importedProfile.EducationSection ??= new();
        importedProfile.SkillsSection ??= new();
        importedProfile.ProjectsSection ??= new();
        importedProfile.LanguagesSection ??= new();
        importedProfile.InterestsSection ??= new();

        foreach (var skill in importedProfile.Skills)
        {
            skill.Id = 0;
            skill.CandidateProfileId = currentProfile.Id;
            skill.CandidateProfile = null;
        }

        foreach (var experience in importedProfile.WorkExperience)
        {
            experience.Id = 0;
            experience.CandidateProfileId = currentProfile.Id;
            experience.CandidateProfile = null;
        }

        foreach (var education in importedProfile.Educations)
        {
            education.Id = 0;
            education.CandidateProfileId = currentProfile.Id;
            education.CandidateProfile = null;
        }

        foreach (var project in importedProfile.Projects)
        {
            project.Id = 0;
            project.CandidateProfileId = currentProfile.Id;
            project.CandidateProfile = null;
        }

        foreach (var language in importedProfile.Languages)
        {
            language.Id = 0;
            language.CandidateProfileId = currentProfile.Id;
            language.CandidateProfile = null;
        }

        foreach (var interest in importedProfile.Interests)
        {
            interest.Id = 0;
            interest.CandidateProfileId = currentProfile.Id;
            interest.CandidateProfile = null;
        }
    }

    private static void EnsureProfileCollections(CandidateProfile profile)
    {
        profile.WorkExperience ??= [];
        profile.Educations ??= [];
        profile.Skills ??= [];
        profile.Projects ??= [];
        profile.Languages ??= [];
        profile.Interests ??= [];
    }

    private sealed class UserDataBackup
    {
        public int Version { get; set; } = 1;
        public DateTime ExportedAtUtc { get; set; }
        public BackupSections Sections { get; set; } = new();
        public CandidateProfile? Profile { get; set; }
        public List<ApplicationBackup> Applications { get; set; } = [];
        public UserSettingsBackup? Settings { get; set; }
        public List<UserAIConfigurationBackup> AIConfigurations { get; set; } = [];
        public List<NoteBackup> Notes { get; set; } = [];
    }

    private sealed class BackupSections
    {
        public bool Profile { get; set; }
        public bool Applications { get; set; }
        public bool Settings { get; set; }
        public bool Notes { get; set; }
    }

    private sealed class ApplicationBackup
    {
        public JobPostingBackup JobPosting { get; set; } = new();
        public string? CoverLetterContent { get; set; }
        public string? TailoredResumeJson { get; set; }
        public string? ApplicationEmailContent { get; set; }
        public string Template { get; set; } = AiCV.Domain.Constants.CvTemplates.Professional;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    private sealed class JobPostingBackup
    {
        public string? Title { get; set; }
        public string? CompanyName { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public DateTime DatePosted { get; set; } = DateTime.Now;
    }

    private sealed class UserSettingsBackup
    {
        public string? OpenAIApiKey { get; set; }
        public string? GoogleGeminiApiKey { get; set; }
        public string? ClaudeApiKey { get; set; }
        public string? GroqApiKey { get; set; }
        public string? DeepSeekApiKey { get; set; }
        public string? OpenRouterApiKey { get; set; }
        public AIProvider DefaultProvider { get; set; } = AIProvider.OpenAI;
        public string? DefaultModelId { get; set; }
    }

    private sealed class UserAIConfigurationBackup
    {
        public AIProvider Provider { get; set; }
        public string? Name { get; set; }
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
        public string? CostType { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    private sealed class NoteBackup
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? Color { get; set; } = "default";
        public bool IsPinned { get; set; }
        public bool IsArchived { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

}
