namespace AiCV.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender<User>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
    {
        var subject = "Reset Your Password - AiCV";
        var heading = "Password Reset Request";
        var messageBody = "<p>Hello,</p><p>We received a request to reset the password for your account. Click the button below to set a new password. This link will expire shortly.</p>";
        var body = GetEmailHtmlLayout(subject, heading, messageBody, "Reset Password", resetLink);
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
    {
        var subject = "Confirm Your Email - AiCV";
        var heading = "Confirm Your Email Address";
        var messageBody = "<p>Hello,</p><p>Thank you for signing up for AiCV! Please click the button below to verify your email address and activate your account.</p>";
        var body = GetEmailHtmlLayout(subject, heading, messageBody, "Confirm Email", confirmationLink);
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
    {
        var subject = "Your Password Reset Code - AiCV";
        var heading = "Password Reset Code";
        var messageBody = $"<p>Hello,</p><p>We received a request to reset your password. Use the following code to complete the request:</p><p style=\"font-size: 32px; font-weight: 800; color: #1e293b; letter-spacing: 4px; text-align: center; margin: 24px 0; background-color: #f1f5f9; padding: 16px; border-radius: 8px; font-family: monospace;\">{resetCode}</p>";
        var body = GetEmailHtmlLayout(subject, heading, messageBody);
        await SendEmailAsync(email, subject, body);
    }

    private string GetEmailHtmlLayout(string title, string heading, string messageBody, string? actionText = null, string? actionUrl = null)
    {
        var buttonHtml = "";
        if (!string.IsNullOrEmpty(actionText) && !string.IsNullOrEmpty(actionUrl))
        {
            buttonHtml = $@"
                <tr>
                    <td align=""center"" style=""padding: 16px 0 12px 0; background-color: #ffffff;"">
                        <table border=""0"" cellspacing=""0"" cellpadding=""0"">
                            <tr>
                                <td align=""center"" style=""border-radius: 6px;"" bgcolor=""#594ae2"">
                                    <a href=""{actionUrl}"" target=""_blank"" style=""font-size: 16px; font-family: system-ui, -apple-system, sans-serif; color: #ffffff; text-decoration: none; border-radius: 6px; padding: 12px 28px; border: 1px solid #594ae2; display: inline-block; font-weight: bold;"">{actionText}</a>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>";
        }

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
</head>
<body style=""margin: 0; padding: 0; background-color: #f8fafc; font-family: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif; -webkit-font-smoothing: antialiased; -moz-osx-font-smoothing: grayscale;"">
    <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""background-color: #f8fafc; padding: 40px 10px;"">
        <tr>
            <td align=""center"">
                <table border=""0"" cellpadding=""0"" cellspacing=""0"" width=""100%"" style=""max-width: 560px; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05); border: 1px solid #e2e8f0;"">
                    <!-- Header -->
                    <tr>
                        <td align=""center"" style=""background: linear-gradient(135deg, #594ae2 0%, #3b2fb3 100%); padding: 32px 20px;"">
                            <h1 style=""margin: 0; font-size: 28px; font-weight: 800; color: #ffffff; letter-spacing: -0.5px; font-family: system-ui, -apple-system, sans-serif;"">AiCV</h1>
                            <p style=""margin: 6px 0 0 0; font-size: 14px; color: rgba(255, 255, 255, 0.85); font-family: system-ui, -apple-system, sans-serif;"">Your AI-Powered Career Assistant</p>
                        </td>
                    </tr>
                    
                    <!-- Body Content -->
                    <tr>
                        <td style=""padding: 40px 32px 24px 32px; background-color: #ffffff;"">
                            <h2 style=""margin: 0 0 16px 0; font-size: 20px; font-weight: 700; color: #1e293b; font-family: system-ui, -apple-system, sans-serif;"">{heading}</h2>
                            <div style=""font-size: 16px; line-height: 1.6; color: #475569; font-family: system-ui, -apple-system, sans-serif;"">
                                {messageBody}
                            </div>
                        </td>
                    </tr>
                    
                    <!-- CTA Button -->
                    {buttonHtml}
                    
                    <!-- Divider & Instructions -->
                    {(!string.IsNullOrEmpty(actionUrl) ? $@"
                    <tr>
                        <td style=""padding: 12px 32px 24px 32px; font-size: 12px; color: #94a3b8; font-family: system-ui, -apple-system, sans-serif; background-color: #ffffff;"">
                            <hr style=""border: none; border-top: 1px solid #f1f5f9; margin: 16px 0;"" />
                            <p style=""margin: 0; word-break: break-all;"">If the button above doesn't work, copy and paste this URL into your browser:</p>
                            <p style=""margin: 6px 0 0 0; word-break: break-all;""><a href=""{actionUrl}"" target=""_blank"" style=""color: #594ae2; text-decoration: underline;"">{actionUrl}</a></p>
                        </td>
                    </tr>" : "")}
                    
                    <!-- Footer -->
                    <tr>
                        <td align=""center"" style=""padding: 24px 32px 32px 32px; background-color: #f8fafc; border-top: 1px solid #f1f5f9; text-align: center;"">
                            <p style=""margin: 0; font-size: 12px; color: #94a3b8; font-family: system-ui, -apple-system, sans-serif;"">This email was sent to you because a request was made from your account.</p>
                            <p style=""margin: 6px 0 0 0; font-size: 12px; color: #94a3b8; font-family: system-ui, -apple-system, sans-serif;"">If you did not make this request, you can safely ignore this email.</p>
                            <p style=""margin: 24px 0 0 0; font-size: 12px; color: #cbd5e1; font-family: system-ui, -apple-system, sans-serif;"">&copy; {DateTime.UtcNow.Year} AiCV. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
";
    }

    private async Task SendEmailAsync(string to, string subject, string htmlMessage)
    {
        var host = _configuration["SMTP_HOST"];
        var portStr = _configuration["SMTP_PORT"];
        var user = _configuration["SMTP_USER"];
        var pass = _configuration["SMTP_PASSWORD"];
        var from = _configuration["SMTP_FROM_EMAIL"] ?? "no-reply@aicv.local";

        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogWarning("SMTP_HOST is not configured. Email to {To} with subject '{Subject}' will NOT be sent physically. BodyLength={BodyLength}", to, subject, htmlMessage?.Length ?? 0);
            return;
        }

        int port = 587;
        if (int.TryParse(portStr, out var parsedPort))
        {
            port = parsedPort;
        }

        try
        {
            using var client = new SmtpClient(host, port);
            client.EnableSsl = _configuration.GetValue<bool>("SMTP_ENABLE_SSL", true);

            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            {
                client.Credentials = new NetworkCredential(user, pass);
            }

            var fromName = _configuration["SMTP_FROM_NAME"] ?? "AiCV";

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(from, fromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(to);

            _logger.LogInformation("Sending email to {To} via SMTP host {Host}:{Port}...", to, host, port);
            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email to {To} sent successfully.", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} via SMTP.", to);
        }
    }
}
