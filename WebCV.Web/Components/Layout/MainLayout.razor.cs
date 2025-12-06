namespace WebCV.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase, IDisposable
{
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] public UserManager<User> UserManager { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public SignInManager<User> SignInManager { get; set; } = default!;
    [Inject] public ILoadingService LoadingService { get; set; } = default!;

    private bool _drawerOpen = true;
    protected bool IsAuthenticated { get; set; }

    public string AppVersion { get; set; } = "1.0.0";

    protected override void OnInitialized()
    {
        LoadingService.OnChange += StateHasChanged;
        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        if (version != null)
        {
            AppVersion = $"{version.Major}.{version.Minor}.{version.Build}";
        }
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
        NavigationManager.NavigateTo(destination, forceLoad: false);
    }

    protected override async Task OnParametersSetAsync()
    {
        // This manual check can cause infinite loops if not handled correctly.
        // We rely on RevalidatingIdentityAuthenticationStateProvider to handle security stamp validation.
        await base.OnParametersSetAsync();
    }
}
