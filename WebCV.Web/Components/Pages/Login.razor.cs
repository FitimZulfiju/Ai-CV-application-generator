namespace WebCV.Web.Components.Pages;

public partial class Login
{
    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [SupplyParameterFromQuery]
    public string? Error { get; set; }

    private string _email = "";
    private string _password = "";
    private bool _rememberMe;
    private bool _showPassword;

    private InputType PasswordInputType => _showPassword ? InputType.Text : InputType.Password;
    private string PasswordIcon =>
        _showPassword ? Icons.Material.Filled.VisibilityOff : Icons.Material.Filled.Visibility;

    private void TogglePasswordVisibility() => _showPassword = !_showPassword;
}
