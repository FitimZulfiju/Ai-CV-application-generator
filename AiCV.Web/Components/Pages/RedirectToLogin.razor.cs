namespace AiCV.Web.Components.Pages;

public partial class RedirectToLogin
{
    protected override void OnInitialized()
    {
        Navigation.NavigateTo($"/{NavUri.LoginPage}", true);
    }
}
