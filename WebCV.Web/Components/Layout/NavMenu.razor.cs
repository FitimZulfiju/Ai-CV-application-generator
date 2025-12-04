namespace WebCV.Web.Components.Layout;

public partial class NavMenu
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private async Task Logout()
    {
        await JSRuntime.InvokeVoidAsync("eval", "document.getElementById('logout-form').submit();");
    }
}
