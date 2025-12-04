namespace WebCV.Web.Components.Pages;

public partial class RedirectToLogin
{
    [Inject] public NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        Navigation.NavigateTo("/login", true);
    }
}
