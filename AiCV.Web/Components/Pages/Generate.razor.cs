namespace AiCV.Web.Components.Pages;

public partial class Generate : IDisposable
{
    private PrintPreviewModal _printPreviewModal = default!;
    private readonly JobPosting _job = new();
    private string _generatedCoverLetter = string.Empty;
    private CandidateProfile? _generatedResume;
    private string? _detectedCompanyName;
    private string? _detectedJobTitle;
    private CandidateProfile? _cachedProfile;
    private bool _isGenerating;
    private bool _isFetching;
    private bool _isPrintingCoverLetter;
    private bool _isPrintingResume;
    private bool _isSaving;
    private bool _isAlreadySaved;
    private string _savedCoverLetter = string.Empty;
    private string _savedResumeJson = string.Empty;
    private string _generatedEmail = string.Empty;
    private string _savedEmail = string.Empty;
    private bool _previewCoverLetter;
    private bool _previewResume = true;
    private string _resumeJson = string.Empty;
    private string _originalResumeJson = string.Empty;
    private bool _manualEntry;
    private bool _includeProfilePicture;

    private static string GetDisplayStyle(bool visible) => visible ? string.Empty : "display:none";

    private MudForm? _form;
    private int? _activeConfigId;
    private List<UserAIConfiguration> _configuredProviders = [];
    private bool _hasConfiguredProvider;
    private bool _showAdvancedEditor;
    private int _splitterSize = 30;
    private int _activeTabIndex;
    private string _selectedTemplateInPreview = Domain.Constants.CvTemplates.Professional;
    private string _previewHtml = string.Empty;
    private string _customPrompt = string.Empty;
    private string _userId = string.Empty;
    private string _draftSnapshot = string.Empty;
    private Timer? _draftSaveTimer;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
    };

    private void OnResumePreviewToggled(bool value)
    {
        _previewResume = value;
        if (_previewResume)
        {
            try
            {
                if (!string.IsNullOrEmpty(_resumeJson))
                {
                    _generatedResume =
                        JsonSerializer.Deserialize<CandidateProfile>(_resumeJson);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"{Localizer["InvalidJson"]}: {ex.Message}", Severity.Error);
                _previewResume = false;
            }
        }
        else
        {
            if (_generatedResume != null)
            {
                _resumeJson = JsonSerializer.Serialize(
                    _generatedResume,
                    _jsonOptions
                );
            }
        }
    }

    private void ResetResumeJson()
    {
        _resumeJson = _originalResumeJson;
        Snackbar.Add(Localizer["ResetToOriginalGeneratedVersion"], Severity.Info);
    }

    private void OnIncludeProfilePictureToggled(bool value)
    {
        _includeProfilePicture = value;

        if (_generatedResume != null && _cachedProfile != null)
        {
            _generatedResume.ProfilePictureUrl = _cachedProfile.ProfilePictureUrl;
            _generatedResume.ShowProfilePicture =
                _includeProfilePicture && !string.IsNullOrEmpty(_cachedProfile.ProfilePictureUrl);
            _resumeJson = JsonSerializer.Serialize(_generatedResume, _jsonOptions);
            _generatedResume = JsonSerializer.Deserialize<CandidateProfile>(
                _resumeJson
            );
        }

        _isAlreadySaved = false;
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
            await RestoreDraftAsync();
        }

        _draftSnapshot = ComputeDraftSnapshot();
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
            Snackbar.Add($"{Localizer["ErrorLoadingAiConfigurations"]}: {ex.Message}", Severity.Error);
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
            Snackbar.Add(Localizer["PleaseEnterUrlFirst"], Severity.Warning);
            return;
        }

        _isFetching = true;
        LoadingService.Show(Localizer["FetchingJobDetails"], 0);

        ClearPreviousJobData();

        try
        {
            LoadingService.Update(20, Localizer["ConnectingToJobSite"]);
            await Task.Delay(300);

            var fetchedJob = await JobOrchestrator.FetchJobDetailsAsync(_job.Url);

            LoadingService.Update(60, Localizer["ParsingContent"]);

            _job.Description = fetchedJob.Description;
            _job.CompanyName = fetchedJob.CompanyName;
            _job.Title = fetchedJob.Title;
            _showAdvancedEditor = true;
            UpdatePreview(_job.Description);

            LoadingService.Update(100, Localizer["Done"]);
            await Task.Delay(200);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"{Localizer["ErrorFetchingJob"]}: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isFetching = false;
            LoadingService.Hide();
            await PersistDraftAsync();
        }
    }

    private void ClearPreviousJobData()
    {
        _job.Description = string.Empty;
        _job.CompanyName = string.Empty;
        _job.Title = string.Empty;
        _generatedCoverLetter = string.Empty;
        _generatedResume = null;
        _resumeJson = string.Empty;
        _originalResumeJson = string.Empty;
        _detectedCompanyName = null;
        _detectedJobTitle = null;
        _previewCoverLetter = false;
        _previewResume = true;
        _previewHtml = string.Empty;
        _customPrompt = string.Empty;
        _activeTabIndex = 0;
        _isAlreadySaved = false;
        _savedCoverLetter = string.Empty;
        _savedResumeJson = string.Empty;

        StateHasChanged();
    }

    private void ResetForNewApplication()
    {
        ClearPreviousJobData();
        _job.Url = string.Empty;
        _generatedEmail = string.Empty;
        _savedEmail = string.Empty;
        _cachedProfile = null;
        _showAdvancedEditor = false;
    }

    private string GetDraftKey() => $"generate-draft-{_userId}";

    private GenerateDraft BuildDraft() =>
        new()
        {
            JobUrl = _job.Url,
            JobTitle = _job.Title,
            JobCompanyName = _job.CompanyName,
            JobDescription = _job.Description,
            CustomPrompt = _customPrompt,
            ManualEntry = _manualEntry,
            SelectedConfigId = _activeConfigId,
            ShowAdvancedEditor = _showAdvancedEditor,
            GeneratedCoverLetter = _generatedCoverLetter,
            GeneratedEmail = _generatedEmail,
            ResumeJson = _resumeJson,
            OriginalResumeJson = _originalResumeJson,
            DetectedCompanyName = _detectedCompanyName,
            DetectedJobTitle = _detectedJobTitle,
            ActiveTabIndex = _activeTabIndex,
            PreviewCoverLetter = _previewCoverLetter,
            PreviewResume = _previewResume,
            IncludeProfilePicture = _includeProfilePicture,
            SelectedTemplateInPreview = _selectedTemplateInPreview,
            IsAlreadySaved = _isAlreadySaved,
            SavedCoverLetter = _savedCoverLetter,
            SavedResumeJson = _savedResumeJson,
            SavedEmail = _savedEmail,
        };

    private string ComputeDraftSnapshot() =>
        JsonSerializer.Serialize(BuildDraft(), _jsonOptions);

    private static bool IsDraftEmpty(GenerateDraft draft) =>
        string.IsNullOrWhiteSpace(draft.JobUrl)
        && string.IsNullOrWhiteSpace(draft.JobDescription)
        && string.IsNullOrWhiteSpace(draft.GeneratedCoverLetter)
        && string.IsNullOrWhiteSpace(draft.ResumeJson);

    private async Task RestoreDraftAsync()
    {
        var draft = await PersistenceService.GetDraftAsync<GenerateDraft>(GetDraftKey());
        if (draft == null || IsDraftEmpty(draft))
        {
            return;
        }

        _job.Url = draft.JobUrl;
        _job.Title = draft.JobTitle;
        _job.CompanyName = draft.JobCompanyName;
        _job.Description = draft.JobDescription;
        _customPrompt = draft.CustomPrompt;
        _manualEntry = draft.ManualEntry;
        _showAdvancedEditor = draft.ShowAdvancedEditor;
        _generatedCoverLetter = draft.GeneratedCoverLetter;
        _generatedEmail = draft.GeneratedEmail;
        _resumeJson = draft.ResumeJson;
        _originalResumeJson = draft.OriginalResumeJson;
        _detectedCompanyName = draft.DetectedCompanyName;
        _detectedJobTitle = draft.DetectedJobTitle;
        _activeTabIndex = draft.ActiveTabIndex;
        _previewCoverLetter = draft.PreviewCoverLetter;
        _previewResume = draft.PreviewResume;
        _includeProfilePicture = draft.IncludeProfilePicture;
        _selectedTemplateInPreview = draft.SelectedTemplateInPreview;
        _isAlreadySaved = draft.IsAlreadySaved;
        _savedCoverLetter = draft.SavedCoverLetter;
        _savedResumeJson = draft.SavedResumeJson;
        _savedEmail = draft.SavedEmail;

        if (
            draft.SelectedConfigId.HasValue
            && _configuredProviders.Any(c => c.Id == draft.SelectedConfigId.Value)
        )
        {
            _activeConfigId = draft.SelectedConfigId;
        }

        UpdatePreview(_job.Description);

        if (!string.IsNullOrEmpty(_resumeJson))
        {
            _cachedProfile = await CVService.GetProfileAsync(_userId);

            if (_previewResume)
            {
                try
                {
                    _generatedResume = JsonSerializer.Deserialize<CandidateProfile>(
                        _resumeJson
                    );
                }
                catch
                {
                    _previewResume = false;
                }
            }
        }
    }

    private async Task PersistDraftAsync()
    {
        if (string.IsNullOrEmpty(_userId))
        {
            return;
        }

        _draftSnapshot = ComputeDraftSnapshot();
        await PersistenceService.SaveDraftAsync(GetDraftKey(), BuildDraft());
    }

    private void CheckDraftState(object? state)
    {
        if (string.IsNullOrEmpty(_userId))
        {
            return;
        }

        var snapshot = ComputeDraftSnapshot();
        if (snapshot == _draftSnapshot)
        {
            return;
        }

        _ = InvokeAsync(PersistDraftAsync);
    }

    private async Task GenerateContent()
    {
        await _form!.ValidateAsync();
        if (!_form.IsValid)
            return;

        _isGenerating = true;
        LoadingService.Show(Localizer["GeneratingApplication"], 0);
        try
        {
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                Snackbar.Add(Localizer["UserIdNotFound"], Severity.Error);
                return;
            }

            LoadingService.Update(10, Localizer["AnalyzingProfile"]);
            _cachedProfile = await CVService.GetProfileAsync(userId);

            if (_cachedProfile == null)
            {
                Snackbar.Add(Localizer["UserProfileNotFound"], Severity.Error);
                return;
            }

            if (
                string.IsNullOrEmpty(_cachedProfile.ProfessionalSummary)
                && _cachedProfile.WorkExperience.Count == 0
            )
            {
                Snackbar.Add(
                    Localizer["YourProfileIsEmpty"],
                    Severity.Warning
                );
                return;
            }

            LoadingService.Update(30, Localizer["GeneratingCoverLetter"]);
            var activeConfig = GetActiveConfiguration();
            if (activeConfig == null)
            {
                Snackbar.Add(
                    Localizer["NoAiConfigurationSelected"],
                    Severity.Warning
                );
                return;
            }

            if (activeConfig.ApiKey == "DECRYPTION_FAILED")
            {
                Snackbar.Add(
                    Localizer["ApiKeyDecryptionFailed"],
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

            LoadingService.Update(70, Localizer["TailoringCv"]);
            _generatedCoverLetter = CoverLetter;
            _generatedResume = ResumeResult.Profile;
            _generatedEmail = ApplicationEmail;

            if (_generatedResume != null && _cachedProfile != null)
            {
                _generatedResume.ProfilePictureUrl = _cachedProfile.ProfilePictureUrl;
                _generatedResume.ShowProfilePicture =
                    _includeProfilePicture
                    && !string.IsNullOrEmpty(_cachedProfile.ProfilePictureUrl);

                _generatedResume.Tagline = _cachedProfile.Tagline;
                _generatedResume.FooterText = _cachedProfile.FooterText;

                _generatedResume.SummarySection = _cachedProfile.SummarySection;
                _generatedResume.ExperienceSection = _cachedProfile.ExperienceSection;
                _generatedResume.EducationSection = _cachedProfile.EducationSection;
                _generatedResume.CoreCompetenciesSection = _cachedProfile.CoreCompetenciesSection;
                _generatedResume.ProjectsSection = _cachedProfile.ProjectsSection;
                _generatedResume.LanguagesSection = _cachedProfile.LanguagesSection;
                _generatedResume.InterestsSection = _cachedProfile.InterestsSection;
            }
            _resumeJson = JsonSerializer.Serialize(_generatedResume, _jsonOptions);
            _originalResumeJson = _resumeJson;
            _detectedCompanyName = ResumeResult.DetectedCompanyName;
            _detectedJobTitle = ResumeResult.DetectedJobTitle;

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
                    string.Format(Localizer["AiDetected"], _detectedCompanyName, _detectedJobTitle),
                    Severity.Info
                );
            }

            LoadingService.Update(100, Localizer["Complete"]);
            await Task.Delay(300);

            Snackbar.Add(Localizer["ApplicationGenerated"], Severity.Success);
            _previewCoverLetter = true;

            if (
                _isAlreadySaved
                && _generatedCoverLetter == _savedCoverLetter
                && _resumeJson == _savedResumeJson
                && _generatedEmail == _savedEmail
            )
            {
                //
            }
            else
            {
                _isAlreadySaved = false;
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"{Localizer["ErrorDiscoveryService"]}: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isGenerating = false;
            LoadingService.Hide();
            await PersistDraftAsync();
        }
    }

    private async Task SaveApplication()
    {
        _isSaving = true;
        StateHasChanged();
        await Task.Yield();

        if (_isAlreadySaved)
        {
            Snackbar.Add(
                Localizer["ApplicationAlreadySaved"],
                Severity.Info
            );
            _isSaving = false;
            StateHasChanged();
            return;
        }

        if (string.IsNullOrEmpty(_generatedCoverLetter))
        {
            Snackbar.Add(Localizer["PleaseGenerateCoverLetterFirst"], Severity.Warning);
            return;
        }

        if (_generatedResume == null)
        {
            Snackbar.Add(Localizer["PleaseGenerateTailoredCvFirst"], Severity.Warning);
            return;
        }

        if (_cachedProfile == null)
        {
            Snackbar.Add(Localizer["ProfileDataMissing"], Severity.Warning);
            return;
        }

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            Snackbar.Add(Localizer["UserIdNotFound"], Severity.Error);
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
                _generatedEmail,
                _selectedTemplateInPreview
            );
            _isAlreadySaved = true;
            _savedCoverLetter = _generatedCoverLetter;
            _savedResumeJson = _resumeJson;
            _savedEmail = _generatedEmail;
            await Task.Yield();
            Snackbar.Add(Localizer["ApplicationSavedSuccessfully"], Severity.Success);

            await PersistenceService.ClearDraftAsync(GetDraftKey());
            ResetForNewApplication();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"{Localizer["ErrorSaving"]}: {ex.Message}", Severity.Error);
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
        Snackbar.Add(Localizer["CopiedToClipboard"], Severity.Success);
    }

    private async Task CopyResumeJson()
    {
        if (_generatedResume == null)
            return;
        var json = JsonSerializer.Serialize(_generatedResume);
        await ClipboardService.CopyToClipboardAsync(json);
        Snackbar.Add(Localizer["CopiedJsonToClipboard"], Severity.Success);
    }

    private async Task PrintResume()
    {
        if (_generatedResume == null)
            return;

        _isPrintingResume = true;
        StateHasChanged();
        await Task.Yield();
        LoadingService.Show(Localizer["GeneratingPdf"], 0);
        try
        {
            var pdfBytes = await PdfService.GenerateCvAsync(
                _generatedResume,
                _selectedTemplateInPreview
            );
            await _printPreviewModal.ShowAsync(pdfBytes, Localizer["ResumeDocumentType"], _job.Title);
        }
            catch (Exception ex)
            {
                Snackbar.Add($"{Localizer["ErrorGeneratingPdf"]}: {ex.Message}", Severity.Error);
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
        LoadingService.Show(Localizer["GeneratingPdf"], 0);
        try
        {
            var pdfBytes = await PdfService.GenerateCoverLetterAsync(
                _generatedCoverLetter,
                _generatedResume,
                _job.Title,
                _job.CompanyName,
                _selectedTemplateInPreview
            );
            await _printPreviewModal.ShowAsync(
                pdfBytes,
                Localizer["CoverLetterDocumentType"],
                string.Format(Localizer["JobAtCompanyTitle"], _job.Title, _job.CompanyName)
            );
        }
        catch (Exception ex)
        {
            Snackbar.Add($"{Localizer["ErrorGeneratingPdf"]}: {ex.Message}", Severity.Error);
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
        public string JobUrl { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string JobCompanyName { get; set; } = string.Empty;
        public string JobDescription { get; set; } = string.Empty;
        public string CustomPrompt { get; set; } = string.Empty;
        public bool ManualEntry { get; set; }
        public int? SelectedConfigId { get; set; }
        public bool ShowAdvancedEditor { get; set; }
        public string GeneratedCoverLetter { get; set; } = string.Empty;
        public string GeneratedEmail { get; set; } = string.Empty;
        public string ResumeJson { get; set; } = string.Empty;
        public string OriginalResumeJson { get; set; } = string.Empty;
        public string? DetectedCompanyName { get; set; }
        public string? DetectedJobTitle { get; set; }
        public int ActiveTabIndex { get; set; }
        public bool PreviewCoverLetter { get; set; }
        public bool PreviewResume { get; set; } = true;
        public bool IncludeProfilePicture { get; set; }
        public string SelectedTemplateInPreview { get; set; } =
            Domain.Constants.CvTemplates.Professional;
        public bool IsAlreadySaved { get; set; }
        public string SavedCoverLetter { get; set; } = string.Empty;
        public string SavedResumeJson { get; set; } = string.Empty;
        public string SavedEmail { get; set; } = string.Empty;
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _draftSaveTimer = new Timer(
                CheckDraftState,
                null,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(2)
            );
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _draftSaveTimer?.Dispose();
        GC.SuppressFinalize(this);
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
