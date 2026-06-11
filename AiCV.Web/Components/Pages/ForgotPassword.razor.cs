namespace AiCV.Web.Components.Pages;

public partial class ForgotPassword
{
    [Inject]
    private ILogger<ForgotPassword> Logger { get; set; } = default!;

    [EmailAddress]
    [Required]
    public string Email { get; set; } = string.Empty;

    private bool _isProcessing = false;
    private bool _success = false;
    private string? _error;
    private string? _devResetLink;

    private async Task HandleForgotPassword()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            _error = "Please enter your email address.";
            return;
        }

        _isProcessing = true;
        _error = null;
        _devResetLink = null;

        try
        {
            var user = await UserManager.FindByEmailAsync(Email);
            if (user != null)
            {
                var token = await UserManager.GeneratePasswordResetTokenAsync(user);
                
                // Construct reset password link
                var resetLink = Navigation.ToAbsoluteUri($"/{NavUri.ResetPasswordPage}?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}").ToString();
                
                Logger.LogInformation("Password reset link generated for {Email}: {Link}", user.Email, resetLink);

                // Send email using the configured IEmailSender service
                await EmailSender.SendPasswordResetLinkAsync(user, user.Email!, resetLink);

                // Show in the UI for development environment to make local testing easy without SMTP
                if (Environment.IsDevelopment())
                {
                    _devResetLink = resetLink;
                }
            }

            // Always display success to prevent email enumeration/harvesting attacks
            _success = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error occurred during forgot password request for {Email}", Email);
            _error = "An error occurred while processing your request.";
        }
        finally
        {
            _isProcessing = false;
        }
    }
}
