namespace AiCV.Web.Components.Layout;

public partial class NavMenu
{
    [Parameter]
    public bool ShowLoggedInAs { get; set; } = true;

    [Parameter]
    public bool IsExpanded { get; set; } = true;

    private string AppVersion { get; } = AppVersionProvider.GetDisplayVersion();

    private string NavMenuShellClass =>
        IsExpanded ? "nav-menu-shell" : "nav-menu-shell nav-menu-shell-collapsed";

    private async Task Logout()
    {
        await JSRuntime.InvokeVoidAsync(
            "eval",
            "var f = document.getElementById('logout-form'); if (f) { f.submit(); }"
        );
    }
}
