namespace AiCV.Web.Components.Pages;

public partial class Login
{
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
