namespace AiCV.Web.Components.Pages;

public partial class Generate
{
    private PrintPreviewModal _printPreviewModal = default!;
    private readonly JobPosting _job = new();
    private string _generatedCoverLetter = string.Empty;
    private CandidateProfile? _generatedResume;
    private string? _detectedCompanyName;
    private string? _detectedJobTitle;
    private CandidateProfile? _cachedProfile;
    private bool _isGenerating = false;
    private bool _isFetching = false;
    private bool _isPrintingCoverLetter = false;
    private bool _isPrintingResume = false;
    private bool _isSaving = false;
    private bool _isAlreadySaved = false;
    private string _savedCoverLetter = string.Empty;
    private string _savedResumeJson = string.Empty;
    private string _generatedEmail = string.Empty;
    private string _savedEmail = string.Empty;
    private bool _previewCoverLetter = false;
    private bool _previewResume = true;
    private string _resumeJson = string.Empty;
    private string _originalResumeJson = string.Empty;
    private bool _manualEntry = false;
    private bool _includeProfilePicture = false;

    private static string GetDisplayStyle(bool visible) => visible ? string.Empty : "display:none";

    private MudForm? _form;
    private int? _activeConfigId;
    private List<UserAIConfiguration> _configuredProviders = [];
    private bool _hasConfiguredProvider = false;
    private bool _showAdvancedEditor = false;
    private int _splitterSize = 30;
    private int _activeTabIndex = 0;
    private string _previewHtml = string.Empty;
    private string _customPrompt = string.Empty;
    private string _userId = string.Empty;
    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
    };

    private void OnResumePreviewToggled(bool value)
    {
        _previewResume = value;
        if (_previewResume)
        {
            // Switch to Preview: Deserialize JSON back to Object
            try
            {
                if (!string.IsNullOrEmpty(_resumeJson))
                {
                    _generatedResume =
                        System.Text.Json.JsonSerializer.Deserialize<CandidateProfile>(_resumeJson);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Invalid JSON: {ex.Message}", Severity.Error);
                _previewResume = false; // Stay in edit mode
            }
        }
        else
        {
            // Switch to Edit: Serialize Object to JSON
            if (_generatedResume != null)
            {
                _resumeJson = System.Text.Json.JsonSerializer.Serialize(
                    _generatedResume,
                    _jsonOptions
                );
            }
        }
    }

    private void ResetResumeJson()
    {
        _resumeJson = _originalResumeJson;
        Snackbar.Add("Reset to original generated version.", Severity.Info);
    }

    private void OnIncludeProfilePictureToggled(bool value)
    {
        _includeProfilePicture = value;

        // Update the generated resume immediately if it exists
        if (_generatedResume != null && _cachedProfile != null)
        {
            // Ensure the profile picture URL is always copied from the master profile
            _generatedResume.ProfilePictureUrl = _cachedProfile.ProfilePictureUrl;
            _generatedResume.ShowProfilePicture =
                _includeProfilePicture && !string.IsNullOrEmpty(_cachedProfile.ProfilePictureUrl);

            // Serialize and deserialize to create a new object reference
            // This forces Blazor to detect the change and re-render the CvPreview component
            _resumeJson = System.Text.Json.JsonSerializer.Serialize(_generatedResume, _jsonOptions);
            _generatedResume = System.Text.Json.JsonSerializer.Deserialize<CandidateProfile>(
                _resumeJson
            );
        }

        // Mark as unsaved since we changed something
        _isAlreadySaved = false;

        // Force UI refresh
        StateHasChanged();
    }

    private bool HasProfilePicture()
    {
        return _cachedProfile != null && !string.IsNullOrEmpty(_cachedProfile.ProfilePictureUrl);
    }

    private void UpdatePreview(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _previewHtml = string.Empty;
        }
        else
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            _previewHtml = Markdown.ToHtml(text, pipeline);
        }
    }

    private void ClearEditor()
    {
        _job.Description = string.Empty;
        UpdatePreview(string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        _userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        if (!string.IsNullOrEmpty(_userId))
        {
            await LoadConfiguredProviders();
        }
    }

    private async Task LoadConfiguredProviders()
    {
        try
        {
            var configs = await ConfigurationService.GetConfigurationsAsync(_userId);
            _configuredProviders =
                configs?.Where(c => !string.IsNullOrEmpty(c.ApiKey)).ToList() ?? [];
            _hasConfiguredProvider = _configuredProviders.Count != 0;

            if (_hasConfiguredProvider && _activeConfigId == null)
            {
                var defaultConf =
                    _configuredProviders.FirstOrDefault(c => c.IsActive) ?? _configuredProviders[0];
                _activeConfigId = defaultConf.Id;
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading AI configurations: {ex.Message}", Severity.Error);
        }
    }

    private UserAIConfiguration? GetActiveConfiguration()
    {
        return _configuredProviders.FirstOrDefault(c => c.Id == _activeConfigId);
    }

    private async Task FetchJobDetails()
    {
        if (string.IsNullOrWhiteSpace(_job.Url))
        {
            Snackbar.Add("Please enter a URL first.", Severity.Warning);
            return;
        }

        _isFetching = true;
        LoadingService.Show("Fetching job details...", 0);

        // Clear all previous data to prevent mixing cached content
        ClearPreviousJobData();

        try
        {
            LoadingService.Update(20, "Connecting to job site...");
            await Task.Delay(300); // Simulate network delay

            var fetchedJob = await JobOrchestrator.FetchJobDetailsAsync(_job.Url);

            LoadingService.Update(60, "Parsing content...");

            _job.Description = fetchedJob.Description;
            _job.CompanyName = fetchedJob.CompanyName;
            _job.Title = fetchedJob.Title;
            _showAdvancedEditor = true;
            UpdatePreview(_job.Description);

            LoadingService.Update(100, "Done!");
            await Task.Delay(200);

            Snackbar.Add("Job details fetched successfully!", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error fetching job: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isFetching = false;
            LoadingService.Hide();
        }
    }

    private void ClearPreviousJobData()
    {
        // Clear job details (except URL which is being used for fetch)
        _job.Description = string.Empty;
        _job.CompanyName = string.Empty;
        _job.Title = string.Empty;

        // Clear generated content
        _generatedCoverLetter = string.Empty;
        _generatedResume = null;
        _resumeJson = string.Empty;
        _originalResumeJson = string.Empty;

        // Clear detected values
        _detectedCompanyName = null;
        _detectedJobTitle = null;

        // Reset preview states
        _previewCoverLetter = false;
        _previewResume = true;
        _previewHtml = string.Empty;
        _customPrompt = string.Empty;

        // Reset to first tab
        _activeTabIndex = 0;

        // Reset saved state and snapshots
        _isAlreadySaved = false;
        _savedCoverLetter = string.Empty;
        _savedResumeJson = string.Empty;

        StateHasChanged();
    }

    private async Task GenerateContent()
    {
        await _form!.Validate();
        if (!_form.IsValid)
            return;

        _isGenerating = true;
        LoadingService.Show("Generating application...", 0);
        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                Snackbar.Add("User ID not found. Please log in again.", Severity.Error);
                return;
            }

            LoadingService.Update(10, "Analyzing profile...");
            _cachedProfile = await CVService.GetProfileAsync(userId);

            if (_cachedProfile == null)
            {
                Snackbar.Add("User profile not found. Please log in again.", Severity.Error);
                return;
            }

            if (
                string.IsNullOrEmpty(_cachedProfile.ProfessionalSummary)
                && _cachedProfile.WorkExperience.Count == 0
            )
            {
                Snackbar.Add(
                    "Your profile is empty! Please go to the Profile page and fill in your details first.",
                    Severity.Warning
                );
                return;
            }

            LoadingService.Update(30, "Generating cover letter...");
            var activeConfig = GetActiveConfiguration();
            if (activeConfig == null)
            {
                Snackbar.Add(
                    "No AI configuration selected. Please configure a provider in Settings.",
                    Severity.Warning
                );
                return;
            }

            if (activeConfig.ApiKey == "DECRYPTION_FAILED")
            {
                Snackbar.Add(
                    "Error: The selected API Key could not be decrypted. Please go to Settings and re-enter your API Key.",
                    Severity.Error
                );
                return;
            }
            var (CoverLetter, ResumeResult, ApplicationEmail) =
                await JobOrchestrator.GenerateApplicationAsync(
                    userId,
                    activeConfig.Provider,
                    _cachedProfile,
                    _job,
                    activeConfig.ModelId,
                    _customPrompt
                );

            LoadingService.Update(70, "Tailoring CV...");
            _generatedCoverLetter = CoverLetter;
            _generatedResume = ResumeResult.Profile;
            _generatedEmail = ApplicationEmail;

            // Copy profile picture settings from the master profile to the tailored CV
            // Use the switch value to determine if the picture should be shown
            if (_generatedResume != null && _cachedProfile != null)
            {
                _generatedResume.ProfilePictureUrl = _cachedProfile.ProfilePictureUrl;
                _generatedResume.ShowProfilePicture =
                    _includeProfilePicture
                    && !string.IsNullOrEmpty(_cachedProfile.ProfilePictureUrl);
            }
            _resumeJson = System.Text.Json.JsonSerializer.Serialize(_generatedResume, _jsonOptions);
            _originalResumeJson = _resumeJson;
            _detectedCompanyName = ResumeResult.DetectedCompanyName;
            _detectedJobTitle = ResumeResult.DetectedJobTitle;

            // Fallback & Correction: Use AI-detected values if missing OR if they differ (AI is usually smarter)
            if (
                !string.IsNullOrWhiteSpace(_detectedCompanyName)
                && (
                    string.IsNullOrWhiteSpace(_job.CompanyName)
                    || !_job.CompanyName.Equals(
                        _detectedCompanyName,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            )
            {
                _job.CompanyName = _detectedCompanyName;
            }

            if (
                !string.IsNullOrWhiteSpace(_detectedJobTitle)
                && (
                    string.IsNullOrWhiteSpace(_job.Title)
                    || !_job.Title.Equals(_detectedJobTitle, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                _job.Title = _detectedJobTitle;
            }

            if (
                !string.IsNullOrWhiteSpace(_detectedCompanyName)
                || !string.IsNullOrWhiteSpace(_detectedJobTitle)
            )
            {
                Snackbar.Add(
                    $"AI Detected: {_detectedCompanyName} - {_detectedJobTitle}",
                    Severity.Info
                );
            }

            LoadingService.Update(100, "Complete!");
            await Task.Delay(300);

            Snackbar.Add("Application Generated!", Severity.Success);
            _previewCoverLetter = true; // Auto-switch to preview

            // Only allow saving if content is different from what was previously saved
            if (
                _isAlreadySaved
                && _generatedCoverLetter == _savedCoverLetter
                && _resumeJson == _savedResumeJson
                && _generatedEmail == _savedEmail
            )
            {
                // Content is the same as saved, keep saved state
            }
            else
            {
                _isAlreadySaved = false; // Allow saving new/different content
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isGenerating = false;
            LoadingService.Hide();
        }
    }

    private async Task SaveApplication()
    {
        _isSaving = true;
        StateHasChanged();
        await Task.Yield();

        // Check if already saved
        if (_isAlreadySaved)
        {
            Snackbar.Add(
                "This application has already been saved. Generate a new application to save again.",
                Severity.Info
            );
            _isSaving = false;
            StateHasChanged();
            return;
        }

        if (string.IsNullOrEmpty(_generatedCoverLetter))
        {
            Snackbar.Add("Please generate a cover letter first.", Severity.Warning);
            return;
        }

        if (_generatedResume == null)
        {
            Snackbar.Add("Please generate a tailored CV first.", Severity.Warning);
            return;
        }

        if (_cachedProfile == null)
        {
            Snackbar.Add("Profile data is missing. Please try generating again.", Severity.Warning);
            return;
        }

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            Snackbar.Add("User ID not found. Please log in again.", Severity.Error);
            return;
        }

        try
        {
            await JobOrchestrator.SaveApplicationAsync(
                userId,
                _job,
                _cachedProfile,
                _generatedCoverLetter,
                _generatedResume!,
                _generatedEmail
            );
            _isAlreadySaved = true;
            // Store what was saved to compare with future generations
            _savedCoverLetter = _generatedCoverLetter;
            _savedResumeJson = _resumeJson;
            _savedEmail = _generatedEmail;
            await Task.Yield();
            Snackbar.Add("Application saved successfully!", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    private async Task CopyToClipboard(string text)
    {
        await ClipboardService.CopyToClipboardAsync(text);
        Snackbar.Add("Copied to clipboard!", Severity.Success);
    }

    private async Task CopyResumeJson()
    {
        if (_generatedResume == null)
            return;
        var json = System.Text.Json.JsonSerializer.Serialize(_generatedResume);
        await ClipboardService.CopyToClipboardAsync(json);
        Snackbar.Add("Copied JSON to clipboard!", Severity.Success);
    }

    private async Task PrintResume()
    {
        if (_generatedResume == null)
            return;

        _isPrintingResume = true;
        StateHasChanged();
        await Task.Yield();
        LoadingService.Show("Generating PDF...", 0);
        try
        {
            var pdfBytes = await PdfService.GenerateCvAsync(_generatedResume);
            await _printPreviewModal.ShowAsync(pdfBytes, "Resume", _job.Title);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error generating PDF: {ex.Message}", Severity.Error);
        }
        finally
        {
            LoadingService.Hide();
            _isPrintingResume = false;
            StateHasChanged();
        }
    }

    private async Task PrintCoverLetter()
    {
        if (string.IsNullOrEmpty(_generatedCoverLetter) || _generatedResume == null)
            return;

        _isPrintingCoverLetter = true;
        StateHasChanged();
        await Task.Yield();
        LoadingService.Show("Generating PDF...", 0);
        try
        {
            var pdfBytes = await PdfService.GenerateCoverLetterAsync(
                _generatedCoverLetter,
                _generatedResume,
                _job.Title,
                _job.CompanyName
            );
            await _printPreviewModal.ShowAsync(
                pdfBytes,
                "Cover Letter",
                $"{_job.Title} at {_job.CompanyName}"
            );
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error generating PDF: {ex.Message}", Severity.Error);
        }
        finally
        {
            LoadingService.Hide();
            _isPrintingCoverLetter = false;
            StateHasChanged();
        }
    }

    public class GenerateDraft
    {
        public JobPosting Job { get; set; } = new();
        public string CustomPrompt { get; set; } = string.Empty;
        public bool ManualEntry { get; set; }
        public int? SelectedConfigId { get; set; }
        public bool ShowAdvancedEditor { get; set; }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) { }
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
