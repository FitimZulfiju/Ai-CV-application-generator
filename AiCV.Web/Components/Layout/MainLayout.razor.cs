namespace AiCV.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase, IDisposable
{
    private const int MobileDrawerBreakpoint = 960;
    private bool _desktopDrawerOpen = true;
    private bool _mobileDrawerOpen;
    private bool _isMobile = true;
    private DotNetObjectReference<MainLayout>? _dotNetReference;
    protected bool IsAuthenticated { get; set; }

    protected override void OnInitialized()
    {
        LoadingService.OnChange += StateHasChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _dotNetReference = DotNetObjectReference.Create(this);
        await JSRuntime.InvokeVoidAsync("aiCvLayoutViewport.register", _dotNetReference, MobileDrawerBreakpoint);
    }

    public void Dispose()
    {
        LoadingService.OnChange -= StateHasChanged;
        _dotNetReference?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void DrawerToggle()
    {
        if (_isMobile)
        {
            _mobileDrawerOpen = !_mobileDrawerOpen;
        }
        else
        {
            _desktopDrawerOpen = !_desktopDrawerOpen;
        }
    }

    [JSInvokable]
    public async Task OnViewportChanged(bool isMobile)
    {
        var hasChanged = _isMobile != isMobile;

        _isMobile = isMobile;

        if (isMobile)
        {
            _mobileDrawerOpen = false;
        }

        if (hasChanged)
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    protected void NavigateToHome()
    {
        var destination = IsAuthenticated ? "/" : "/";
        Navigation.NavigateTo(destination, forceLoad: false);
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
    }
}
