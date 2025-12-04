namespace WebCV.Web.Components.Pages;

public partial class ApplicationDetails
{
    [Inject] public ICVService CVService { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Parameter] public int Id { get; set; }

    private GeneratedApplication? _application;
    private CandidateProfile? _tailoredResume;
    private CandidateProfile? _cachedProfile;
    private bool _isLoading = true;

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
}
