namespace WebCV.Web.Components.Pages;

public partial class Generate
{
    [Inject] public ICVService CVService { get; set; } = default!;
    [Inject] public IJobApplicationOrchestrator JobOrchestrator { get; set; } = default!;
    [Inject] public IClipboardService ClipboardService { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] public IUserSettingsService UserSettingsService { get; set; } = default!;
    [Inject] public ILoadingService LoadingService { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    private JobPosting _job = new();
    private string _generatedCoverLetter = string.Empty;
    private CandidateProfile? _generatedResume;
    private string? _detectedCompanyName;
    private string? _detectedJobTitle;
    private CandidateProfile? _cachedProfile;
    private bool _isGenerating = false;
    private bool _isFetching = false;
    private bool _previewCoverLetter = false;
    private bool _previewResume = true;
    private string _resumeJson = string.Empty;
    private string _originalResumeJson = string.Empty;
    private bool _manualEntry = false;
    private MudForm? _form;
    private AIProvider _selectedProvider = AIProvider.OpenAI;
    private bool _showAdvancedEditor = false;
    private int _splitterSize = 30;
    private int _activeTabIndex = 0;
    private string _previewHtml = string.Empty;

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
                    _generatedResume = System.Text.Json.JsonSerializer.Deserialize<CandidateProfile>(_resumeJson);
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
                _resumeJson = System.Text.Json.JsonSerializer.Serialize(_generatedResume, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            }
        }
    }

    private void ResetResumeJson()
    {
        _resumeJson = _originalResumeJson;
        Snackbar.Add("Reset to original generated version.", Severity.Info);
    }

    private void UpdatePreview(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _previewHtml = string.Empty;
        }
        else
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
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
        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            var settings = await UserSettingsService.GetUserSettingsAsync(userId);
            if (settings != null && !string.IsNullOrEmpty(settings.DefaultModel))
            {
                if (settings.DefaultModel.StartsWith("gpt", StringComparison.OrdinalIgnoreCase))
                {
                    _selectedProvider = AIProvider.OpenAI;
                }
                else if (settings.DefaultModel.StartsWith("gemini", StringComparison.OrdinalIgnoreCase))
                {
                    _selectedProvider = AIProvider.GoogleGemini;
                }
            }
        }
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
        try
        {
            LoadingService.Update(20, "Connecting to job site...");
            await Task.Delay(300); // Simulate network delay

            var fetchedJob = await JobOrchestrator.FetchJobDetailsAsync(_job.Url);
            
            LoadingService.Update(60, "Parsing content...");
            
            _job.Description = fetchedJob.Description;
            _showAdvancedEditor = true;
            UpdatePreview(_job.Description);
            // Only overwrite title/company if they are generic placeholders
            if (string.IsNullOrWhiteSpace(_job.CompanyName)) _job.CompanyName = fetchedJob.CompanyName;
            if (string.IsNullOrWhiteSpace(_job.Title)) _job.Title = fetchedJob.Title;

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

    private async Task GenerateContent()
    {
        await _form!.Validate();
        if (!_form.IsValid) return;

        _isGenerating = true;
        LoadingService.Show("Generating application...", 0);
        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

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

            if (string.IsNullOrEmpty(_cachedProfile.ProfessionalSummary) && _cachedProfile.WorkExperience.Count == 0)
            {
                Snackbar.Add("Your profile is empty! Please go to the Profile page and fill in your details first.", Severity.Warning);
                return;
            }

            LoadingService.Update(30, "Generating cover letter...");
            var result = await JobOrchestrator.GenerateApplicationAsync(userId, _selectedProvider, _cachedProfile, _job);

            LoadingService.Update(70, "Tailoring CV...");
            _generatedCoverLetter = result.CoverLetter;
            _generatedResume = result.ResumeResult.Profile;
            _resumeJson = System.Text.Json.JsonSerializer.Serialize(_generatedResume, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            _originalResumeJson = _resumeJson;
            _detectedCompanyName = result.ResumeResult.DetectedCompanyName;
            _detectedJobTitle = result.ResumeResult.DetectedJobTitle;

            // Fallback & Correction: Use AI-detected values if missing OR if they differ (AI is usually smarter)
            if (!string.IsNullOrWhiteSpace(_detectedCompanyName) &&
                (string.IsNullOrWhiteSpace(_job.CompanyName) || !_job.CompanyName.Equals(_detectedCompanyName, StringComparison.OrdinalIgnoreCase)))
            {
                _job.CompanyName = _detectedCompanyName;
            }

            if (!string.IsNullOrWhiteSpace(_detectedJobTitle) &&
                (string.IsNullOrWhiteSpace(_job.Title) || !_job.Title.Equals(_detectedJobTitle, StringComparison.OrdinalIgnoreCase)))
            {
                _job.Title = _detectedJobTitle;
            }

            if (!string.IsNullOrWhiteSpace(_detectedCompanyName) || !string.IsNullOrWhiteSpace(_detectedJobTitle))
            {
                Snackbar.Add($"AI Detected: {_detectedCompanyName} - {_detectedJobTitle}", Severity.Info);
            }

            LoadingService.Update(100, "Complete!");
            await Task.Delay(300);

            Snackbar.Add($"Application Generated using {_selectedProvider}!", Severity.Success);
            _previewCoverLetter = true; // Auto-switch to preview

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
        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            Snackbar.Add("User ID not found. Please log in again.", Severity.Error);
            return;
        }

        try
        {
            await JobOrchestrator.SaveApplicationAsync(userId, _job, _cachedProfile, _generatedCoverLetter, _generatedResume!);
            Snackbar.Add("Application saved successfully!", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving: {ex.Message}", Severity.Error);
        }
    }

    private async Task CopyToClipboard(string text)
    {
        await ClipboardService.CopyToClipboardAsync(text);
        Snackbar.Add("Copied to clipboard!", Severity.Success);
    }

    private async Task CopyResumeJson()
    {
        if (_generatedResume == null) return;
        var json = System.Text.Json.JsonSerializer.Serialize(_generatedResume);
        await ClipboardService.CopyToClipboardAsync(json);
        Snackbar.Add("Copied JSON to clipboard!", Severity.Success);
    }

    private async Task PrintResume()
    {
        if (_generatedResume == null) return;
        await JSRuntime.InvokeVoidAsync("window.print");
    }

    private async Task PrintCoverLetter()
    {
        if (string.IsNullOrEmpty(_generatedCoverLetter)) return;
        await JSRuntime.InvokeVoidAsync("window.print");
    }

    private string GetDisplayStyle(bool visible) => visible ? string.Empty : "display:none";
}
