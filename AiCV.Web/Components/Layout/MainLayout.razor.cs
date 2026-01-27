namespace AiCV.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase, IDisposable
{
    private bool _drawerOpen = true;
    protected bool IsAuthenticated { get; set; }
    //private bool _showBetaWarning = true;

    protected override void OnInitialized()
    {
        LoadingService.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        LoadingService.OnChange -= StateHasChanged;
        GC.SuppressFinalize(this);
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    protected void NavigateToHome()
    {
        var destination = IsAuthenticated ? "/" : "/";
        Navigation.NavigateTo(destination, forceLoad: false);
    }

    protected override async Task OnParametersSetAsync()
    {
        // This manual check can cause infinite loops if not handled correctly.
        // We rely on RevalidatingIdentityAuthenticationStateProvider to handle security stamp validation.
        await base.OnParametersSetAsync();
    }
}
