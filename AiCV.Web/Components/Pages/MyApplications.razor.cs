namespace AiCV.Web.Components.Pages;

public partial class MyApplications
{
    private List<GeneratedApplication> _applications = [];
    private HashSet<GeneratedApplication> _selectedItems = [];
    private bool _isLoading = true;
    private string _userId = string.Empty;
    private int? _deletingId;
    private bool _isDeletingMultiple = false;
    private string searchString1 = "";

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        LoadingService.Show(Localizer["LoadingApplications"], 0);
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        try
        {
            if (user.Identity?.IsAuthenticated == true)
            {
                _userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                if (!string.IsNullOrEmpty(_userId))
                {
                    await LoadApplications();
                }
            }
        }
        finally
        {
            _isLoading = false;
            LoadingService.Hide();
        }
    }

    private async Task LoadApplications()
    {
        try
        {
            _applications = await CVService.GetApplicationsAsync(_userId);
        }
        catch (Exception ex)
        {
            Snackbar.Add(Localizer["ErrorLoadingApplications", ex.Message], Severity.Error);
        }
    }

    private void ViewApplication(int id)
    {
        Navigation.NavigateTo($"/{NavUri.ApplicationPage}/{id}");
    }

    private async Task DeleteApplication(GeneratedApplication app)
    {
        bool? result = await DialogService.ShowMessageBoxAsync(
            Localizer["DeleteApplicationTitle"],
            Localizer["DeleteApplicationConfirm", app.JobPosting?.CompanyName ?? string.Empty],
            yesText: Localizer["Delete"],
            cancelText: Localizer["Cancel"]
        );

        if (result == true)
        {
            _deletingId = app.Id;
            StateHasChanged();
            await Task.Yield();

            try
            {
                await CVService.DeleteApplicationAsync(app.Id);
                _applications.Remove(app);

            }
            catch (Exception ex)
            {
                Snackbar.Add(Localizer["ErrorDeletingApplication", ex.Message], Severity.Error);
            }
            finally
            {
                _deletingId = null;
                StateHasChanged();
            }
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (_selectedItems.Count == 0) return;

        bool? result = await DialogService.ShowMessageBoxAsync(
            Localizer["DeleteApplications"],
            string.Format(Localizer["ConfirmDeleteMultipleApps"], _selectedItems.Count),
            yesText: Localizer["Delete"],
            cancelText: Localizer["Cancel"]
        );

        if (result == true)
        {
            _isDeletingMultiple = true;
            StateHasChanged();

            try
            {
                var ids = _selectedItems.Select(x => x.Id).ToList();
                await CVService.DeleteApplicationsAsync(ids);
                _applications.RemoveAll(a => ids.Contains(a.Id));
                _selectedItems.Clear();
                Snackbar.Add(string.Format(Localizer["MultipleApplicationsDeleted"], ids.Count), Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"{Localizer["ErrorDeletingApplications"]}: {ex.Message}", Severity.Error);
            }
            finally
            {
                _isDeletingMultiple = false;
                StateHasChanged();
            }
        }
    }

    private bool FilterFunc1(GeneratedApplication element) => FilterFunc(element, searchString1);

    private static bool FilterFunc(GeneratedApplication element, string searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString))
        {
            return true;
        }

        if (element.JobPosting?.CompanyName?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        if (element.JobPosting?.Title?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        if (element.Status?.Contains(searchString, StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        if (element.Template.Contains(searchString, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if ($"{element.JobPosting?.CompanyName} {element.JobPosting?.Title}".Contains(searchString))
        {
            return true;
        }

        return false;
    }

    private static Color GetStatusColor(string? status)
    {
        return status switch
        {
            ApplicationStatus.PendingReview => Color.Warning,
            ApplicationStatus.Approved => Color.Info,
            ApplicationStatus.AutoApplied => Color.Success,
            ApplicationStatus.Applied => Color.Success,
            ApplicationStatus.Rejected => Color.Error,
            ApplicationStatus.Archived => Color.Default,
            _ => Color.Default,
        };
    }
}
