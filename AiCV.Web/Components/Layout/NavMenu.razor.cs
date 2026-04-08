namespace AiCV.Web.Components.Layout;

public partial class NavMenu
{
    [Parameter]
    public bool ShowLoggedInAs { get; set; } = true;

    private async Task Logout()
    {
        await JSRuntime.InvokeVoidAsync(
            "eval",
            "var f = document.getElementById('logout-form'); if (f) { f.submit(); }"
        );
    }
}
