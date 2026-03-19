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
                                "Warning: User profile not found. Cover letter preview may be incomplete.",
                                Severity.Warning
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error loading profile: {ex.Message}", Severity.Warning);
                }
            }

            if (_application != null && !string.IsNullOrEmpty(_application.TailoredResumeJson))
            {
                try
                {
                    _tailoredResume = System.Text.Json.JsonSerializer.Deserialize<CandidateProfile>(
                        _application.TailoredResumeJson
                    );
                }
                catch (Exception ex)
                {
                    Snackbar.Add(
                        $"Error deserializing tailored CV: {ex.Message}",
                        Severity.Warning
                    );
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading application: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
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
                _application.JobPosting?.Title ?? "Job",
                _application.JobPosting?.CompanyName ?? "Company",
                _application.Template
            );
            await _printPreviewModal.ShowAsync(
                pdfBytes,
                "Cover Letter",
                $"{_application.JobPosting?.Title} at {_application.JobPosting?.CompanyName}"
            );
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error generating PDF: {ex.Message}", Severity.Error);
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
                _application?.Template ?? CvTemplate.Professional
            );
            await _printPreviewModal.ShowAsync(
                pdfBytes,
                "Resume",
                $"{_application?.JobPosting?.Title ?? "Job"} at {_application?.JobPosting?.CompanyName ?? "Company"}"
            );
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error generating PDF: {ex.Message}", Severity.Error);
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
        Snackbar.Add("Copied to clipboard!", Severity.Success);
    }

    private async Task OnTemplateSelected()
    {
        if (_application == null)
            return;

        StateHasChanged();

        try
        {
            await CVService.SaveApplicationAsync(_application);
            Snackbar.Add(Localizer["TemplateUpdated"], Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving template: {ex.Message}", Severity.Error);
        }
    }
}
