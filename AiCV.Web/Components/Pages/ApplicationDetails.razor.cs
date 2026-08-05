namespace AiCV.Web.Components.Pages;

public partial class ApplicationDetails
{
    private PrintPreviewModal _printPreviewModal = default!;

    [Parameter]
    public int Id { get; set; }

    private GeneratedApplication? _application;
    private CandidateProfile? _tailoredResume;
    private CandidateProfile? _cachedProfile;
    private bool _isLoading = true;
    private bool _isPrintingCoverLetter = false;
    private bool _isPrintingResume = false;
    private int _activeTabIndex = 0;
    private int _previousId;
    private bool _includeProfilePicture;

    protected override async Task OnInitializedAsync()
    {
        _previousId = Id;
        await LoadApplicationAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Id != _previousId)
        {
            _previousId = Id;
            await LoadApplicationAsync();
        }
    }

    private async Task LoadApplicationAsync()
    {
        _application = null;
        _tailoredResume = null;
        _cachedProfile = null;
        _isLoading = true;
        _activeTabIndex = 0;
        StateHasChanged();
        LoadingService.Show(Localizer["LoadingApplication"], 0);

        try
        {
            _application = await CVService.GetApplicationAsync(Id);

            if (_application != null)
            {
                try
                {
                    var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                    var user = authState.User;
                    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (!string.IsNullOrEmpty(userId))
                    {
                        _cachedProfile = await CVService.GetProfileAsync(userId);
                        if (_cachedProfile == null)
                        {
                            Snackbar.Add(
                                Localizer["WarningUserProfileNotFoundCoverPreviewIncomplete"],
                                Severity.Warning
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"{Localizer["ErrorLoadingProfile"]}: {ex.Message}", Severity.Warning);
                }
            }

            if (_application != null && !string.IsNullOrEmpty(_application.TailoredResumeJson))
            {
                try
                {
                    _tailoredResume = System.Text.Json.JsonSerializer.Deserialize<CandidateProfile>(
                        _application.TailoredResumeJson
                    );
                    if (_tailoredResume != null)
                    {
                        _includeProfilePicture = _tailoredResume.ShowProfilePicture;
                    }
                }
                catch (Exception ex)
                {
                    Snackbar.Add(
                        $"{Localizer["ErrorDeserializingTailoredCv"]}: {ex.Message}",
                        Severity.Warning
                    );
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"{Localizer["ErrorLoadingApplication"]}: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            LoadingService.Hide();
        }
    }

    private async Task PrintCoverLetter()
    {
        if (_application == null || string.IsNullOrEmpty(_application.CoverLetterContent))
            return;

        _isPrintingCoverLetter = true;
        StateHasChanged();
        await Task.Yield();
        try
        {
            var profile = _tailoredResume ?? _cachedProfile;
            if (profile == null)
                return;

            var pdfBytes = await PdfService.GenerateCoverLetterAsync(
                _application.CoverLetterContent,
                profile,
                _application.JobPosting?.Title ?? Localizer["JobFallback"],
                _application.JobPosting?.CompanyName ?? Localizer["CompanyFallback"],
                _application.Template
            );
            await _printPreviewModal.ShowAsync(
                pdfBytes,
                Localizer["CoverLetterDocumentType"],
                string.Format(Localizer["JobAtCompanyTitle"], _application.JobPosting?.Title, _application.JobPosting?.CompanyName)
            );
        }
        catch (Exception ex)
        {
            Snackbar.Add($"{Localizer["ErrorGeneratingPdf"]}: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isPrintingCoverLetter = false;
            StateHasChanged();
        }
    }

    private async Task PrintResume()
    {
        if (_tailoredResume == null)
            return;

        _isPrintingResume = true;
        StateHasChanged();
        await Task.Yield();
        try
        {
            var pdfBytes = await PdfService.GenerateCvAsync(
                _tailoredResume,
                _application?.Template ?? AiCV.Domain.Constants.CvTemplates.Professional
            );
            await _printPreviewModal.ShowAsync(
                pdfBytes,
                Localizer["ResumeDocumentType"],
                string.Format(
                    Localizer["JobAtCompanyTitle"],
                    _application?.JobPosting?.Title ?? Localizer["JobFallback"],
                    _application?.JobPosting?.CompanyName ?? Localizer["CompanyFallback"]
                )
            );
        }
        catch (Exception ex)
        {
            Snackbar.Add($"{Localizer["ErrorGeneratingPdf"]}: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isPrintingResume = false;
            StateHasChanged();
        }
    }

    private async Task CopyToClipboard(string text)
    {
        await ClipboardService.CopyToClipboardAsync(text);
        Snackbar.Add(Localizer["CopiedToClipboard"], Severity.Success);
    }

    private bool HasProfilePicture()
    {
        return _cachedProfile != null && !string.IsNullOrEmpty(_cachedProfile.ProfilePictureUrl);
    }

    private async Task OnIncludeProfilePictureToggled(bool value)
    {
        _includeProfilePicture = value;

        if (_tailoredResume != null && _cachedProfile != null)
        {
            _tailoredResume.ProfilePictureUrl = _cachedProfile.ProfilePictureUrl;
            _tailoredResume.ShowProfilePicture =
                _includeProfilePicture && !string.IsNullOrEmpty(_cachedProfile.ProfilePictureUrl);

            if (_application != null)
            {
                _application.TailoredResumeJson = System.Text.Json.JsonSerializer.Serialize(_tailoredResume);
                try
                {
                    await CVService.SaveApplicationAsync(_application);
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"{Localizer["ErrorSavingTemplate"]}: {ex.Message}", Severity.Error);
                }
            }
        }

        StateHasChanged();
    }

    private async Task OnTemplateSelected()
    {
        if (_application == null)
            return;

        StateHasChanged();

        try
        {
            await CVService.SaveApplicationAsync(_application);

        }
        catch (Exception ex)
        {
            Snackbar.Add($"{Localizer["ErrorSavingTemplate"]}: {ex.Message}", Severity.Error);
        }
    }
}
