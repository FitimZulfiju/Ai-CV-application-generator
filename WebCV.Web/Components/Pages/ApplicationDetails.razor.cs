namespace WebCV.Web.Components.Pages;

public partial class ApplicationDetails
{
    [Inject] public ICVService CVService { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] public IDialogService DialogService { get; set; } = default!;

    private WebCV.Web.Components.Shared.PrintPreviewModal _printPreviewModal = default!;

    [Parameter] public int Id { get; set; }

    private GeneratedApplication? _application;
    private CandidateProfile? _tailoredResume;
    private CandidateProfile? _cachedProfile;
    private bool _isLoading = true;
    private int _activeTabIndex = 0;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _application = await CVService.GetApplicationAsync(Id);
            
            // Load the original profile for cover letter preview
            if (_application != null)
            {
                try
                {
                    // Get current authenticated user's ID
                    var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                    var user = authState.User;
                    var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    
                    if (!string.IsNullOrEmpty(userId))
                    {
                        _cachedProfile = await CVService.GetProfileAsync(userId);
                        if (_cachedProfile == null)
                        {
                             // Profile not found (Ghost User), but we can still view the application details.
                             // Just won't be able to preview cover letter with personal details.
                             Snackbar.Add("Warning: User profile not found. Cover letter preview may be incomplete.", Severity.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error loading profile: {ex.Message}", Severity.Warning);
                }
            }
            
            // Deserialize the tailored resume JSON
            if (_application != null && !string.IsNullOrEmpty(_application.TailoredResumeJson))
            {
                try
                {
                    _tailoredResume = System.Text.Json.JsonSerializer.Deserialize<CandidateProfile>(_application.TailoredResumeJson);
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error deserializing tailored CV: {ex.Message}", Severity.Warning);
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

    [Inject] public IPdfService PdfService { get; set; } = default!;

    private async Task PrintCoverLetter()
    {
        if (_application == null || string.IsNullOrEmpty(_application.CoverLetterContent)) return;
        
        try
        {
            var profile = _tailoredResume ?? _cachedProfile;
            if (profile == null) return;

             var pdfBytes = await PdfService.GenerateCoverLetterAsync(_application.CoverLetterContent, profile, _application.JobPosting?.Title ?? "Job", _application.JobPosting?.CompanyName ?? "Company");
             await _printPreviewModal.ShowAsync(pdfBytes, "Cover Letter", $"{_application.JobPosting?.Title} at {_application.JobPosting?.CompanyName}");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error generating PDF: {ex.Message}", Severity.Error);
        }
    }

    private async Task PrintResume()
    {
        if (_tailoredResume == null) return;
        
        try
        {
            var pdfBytes = await PdfService.GenerateCvAsync(_tailoredResume);
            await _printPreviewModal.ShowAsync(pdfBytes, "Resume", $"{_application?.JobPosting?.Title ?? "Job"} at {_application?.JobPosting?.CompanyName ?? "Company"}");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error generating PDF: {ex.Message}", Severity.Error);
        }
    }
}
