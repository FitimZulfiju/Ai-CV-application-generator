namespace AiCV.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase, IDisposable
{
    private const int MobileDrawerBreakpoint = 960;
    private bool _desktopDrawerOpen = true;
    private bool _mobileDrawerOpen;
    private bool _isMobile = true;
    private DotNetObjectReference<MainLayout>? _dotNetReference;
    private IReadOnlyList<BreadcrumbItem> _breadcrumbs = [];
    protected bool IsAuthenticated { get; set; }

    protected override void OnInitialized()
    {
        LoadingService.OnChange += StateHasChanged;
        Navigation.LocationChanged += OnLocationChanged;
        UpdateBreadcrumbs();
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
        Navigation.LocationChanged -= OnLocationChanged;
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

    private void OnLocationChanged(object? _, LocationChangedEventArgs _1)
    {
        UpdateBreadcrumbs();
        _ = InvokeAsync(StateHasChanged);
    }

    private void UpdateBreadcrumbs()
    {
        var relativePath = Navigation.ToBaseRelativePath(Navigation.Uri);
        var pathOnly = relativePath.Split(['?', '#'], 2)[0].Trim('/');
        var segments = pathOnly.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var items = new List<BreadcrumbItem>
        {
            new(Localizer["Home"], "/")
        };

        if (segments.Length == 0)
        {
            items[0] = new BreadcrumbItem(Localizer["Home"], "/", true);
            _breadcrumbs = items;
            return;
        }

        if (string.Equals(segments[0], NavUri.ApplicationPage, StringComparison.OrdinalIgnoreCase))
        {
            items.Add(new BreadcrumbItem(Localizer["MyApplications"], $"/{NavUri.MyApplicationsPage}"));
            items.Add(new BreadcrumbItem(Localizer["JobDetails"], $"/{pathOnly}", true));
            _breadcrumbs = items;
            return;
        }

        var currentPath = string.Empty;
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            currentPath = $"{currentPath}/{segment}";
            var isLast = i == segments.Length - 1;

            items.Add(new BreadcrumbItem(GetBreadcrumbLabel(segment), currentPath, isLast));
        }

        _breadcrumbs = items;
    }

    private string GetBreadcrumbLabel(string segment) =>
        segment.ToLowerInvariant() switch
        {
            NavUri.ProfilePage => Localizer["Profile"],
            NavUri.GeneratePage => Localizer["Generate"],
            NavUri.MyApplicationsPage => Localizer["MyApplications"],
            NavUri.SettingsPage => Localizer["Settings"],
            NavUri.NotesPage => Localizer["Notes"],
            NavUri.AdminDashboardPage => Localizer["AdminDashboard"],
            NavUri.AdminLogsPage => Localizer["SystemLogs"],
            NavUri.LoginPage => Localizer["Login"],
            NavUri.RegisterPage => Localizer["Register"],
            _ => ToFriendlySegment(segment)
        };

    private static string ToFriendlySegment(string segment)
    {
        var normalized = segment.Replace('-', ' ').Replace('_', ' ');
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(normalized);
    }
}
