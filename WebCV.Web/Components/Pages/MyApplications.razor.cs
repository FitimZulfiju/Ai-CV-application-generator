namespace WebCV.Web.Components.Pages;

public partial class MyApplications
{
    [Inject] public ICVService CVService { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public IDialogService DialogService { get; set; } = default!;

    private List<GeneratedApplication> _applications = new();
    private bool _isLoading = true;
    private string _userId = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            _userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (!string.IsNullOrEmpty(_userId))
            {
                await LoadApplications();
            }
        }
        _isLoading = false;
    }

    private async Task LoadApplications()
    {
        try
        {
            _applications = await CVService.GetApplicationsAsync(_userId);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading applications: {ex.Message}", Severity.Error);
        }
    }

    private void ViewApplication(int id)
    {
        NavigationManager.NavigateTo($"/application/{id}");
    }

    private async Task DeleteApplication(GeneratedApplication app)
    {
        bool? result = await DialogService.ShowMessageBox(
            "Delete Application", 
            $"Are you sure you want to delete the application for {app.JobPosting?.CompanyName}?", 
            yesText: "Delete", cancelText: "Cancel");

        if (result == true)
        {
            try
            {
                await CVService.DeleteApplicationAsync(app.Id);
                _applications.Remove(app);
                Snackbar.Add("Application deleted", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error deleting application: {ex.Message}", Severity.Error);
            }
        }
    }
}
