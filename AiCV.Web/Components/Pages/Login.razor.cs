namespace AiCV.Web.Components.Pages;

public partial class Login
{
    [SupplyParameterFromQuery]
    public string? Error { get; set; }

    [SupplyParameterFromQuery(Name = "email")]
    public string? SuppliedEmail { get; set; }

    [SupplyParameterFromQuery(Name = "resetSuccess")]
    public bool? ResetSuccess { get; set; }

    private string _email = string.Empty;
    private string _password = string.Empty;

    protected override void OnInitialized()
    {
        if (!string.IsNullOrEmpty(SuppliedEmail))
        {
            _email = SuppliedEmail;
        }
    }

    private void FillDemoCredentials()
    {
        _email = AiCV.Application.Common.DemoConstants.DemoUserEmail;
        _password = AiCV.Application.Common.DemoConstants.DemoUserPassword;
    }
    private bool _rememberMe;
    private bool _showPassword;

    private InputType PasswordInputType => _showPassword ? InputType.Text : InputType.Password;
    private string PasswordIcon =>
        _showPassword ? Icons.Material.Filled.VisibilityOff : Icons.Material.Filled.Visibility;

    private void TogglePasswordVisibility() => _showPassword = !_showPassword;

    // Check if OAuth providers are configured (with real values, not placeholders)
    private bool IsConfigured(string key)
    {
        var value = Configuration[key];
        return !string.IsNullOrEmpty(value)
            && !value.StartsWith("your_", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsGoogleConfigured => IsConfigured("Authentication:Google:ClientId");
    private bool IsMicrosoftConfigured => IsConfigured("Authentication:Microsoft:ClientId");
    private bool IsGitHubConfigured => IsConfigured("Authentication:GitHub:ClientId");
    private bool HasAnyExternalProvider =>
        IsGoogleConfigured || IsMicrosoftConfigured || IsGitHubConfigured;
}
