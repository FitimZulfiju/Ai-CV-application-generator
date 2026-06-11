namespace AiCV.Web.Components.Pages;

public partial class ResetPassword
{
    [Inject]
    private ILogger<ResetPassword> Logger { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "email")]
    public string Email { get; set; } = string.Empty;

    [SupplyParameterFromQuery(Name = "token")]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;

    private bool _isProcessing = false;
    private string? _error;

    protected override void OnInitialized()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Token))
        {
            _error = "Invalid or expired password reset link. Please request a new link.";
        }
    }

    private async Task HandleResetPassword()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Token))
        {
            _error = "Invalid or expired password reset link. Please request a new link.";
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            _error = "Passwords do not match.";
            return;
        }

        _isProcessing = true;
        _error = null;

        try
        {
            var user = await UserManager.FindByEmailAsync(Email);
            if (user == null)
            {
                // To avoid email enumeration/harvesting attacks, redirect to login page with success anyway
                Navigation.NavigateTo($"/{NavUri.LoginPage}?resetSuccess=true");
                return;
            }

            var result = await UserManager.ResetPasswordAsync(user, Token, NewPassword);
            if (result.Succeeded)
            {
                Logger.LogInformation("Successfully reset password for user: {UserId}", user.Id);
                Navigation.NavigateTo($"/{NavUri.LoginPage}?resetSuccess=true");
            }
            else
            {
                _error = string.Join(" ", result.Errors.Select(e => e.Description));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error resetting password.");
            _error = "An error occurred while resetting your password. Please try again.";
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private static string SanitizeForLog(string? value)
    {
        return (value ?? string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);
    }
}
