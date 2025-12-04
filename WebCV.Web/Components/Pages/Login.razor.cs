namespace WebCV.Web.Components.Pages;

public partial class Login
{
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? Error { get; set; }

    private string _email = "";
    private string _password = "";
    private bool _rememberMe;
}
