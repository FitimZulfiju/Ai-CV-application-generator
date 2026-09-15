namespace AiCV.Web.Components.Pages;

public partial class UserSmtpSettingsPage : ComponentBase
{
    private UserSmtpSettings _settings = new();
    private string _userId = string.Empty;
    private bool _isLoading = true;
    private bool _isSaving = false;
    private bool _isSaved = false;

    private bool _isTestingConnection = false;
    private string? _testResult;
    private string? _testErrorDetails;
    private bool _testSuccess = false;

    private MudBlazor.MudForm _form = default!;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        if (!string.IsNullOrEmpty(_userId))
        {
            var savedSettings = await SmtpSettingsService.GetForUserAsync(_userId);
            if (savedSettings != null)
            {
                _settings = savedSettings;
            }
        }
        _isLoading = false;
    }

    private async Task SaveSettings()
    {
        await _form.ValidateAsync();
        if (!_form.IsValid) return;

        _isSaving = true;
        _isSaved = false;

        try
        {
            _settings = await SmtpSettingsService.UpdateAsync(_userId, _settings);
            _isSaved = true;
            _testResult = null;
        }
        finally
        {
            _isSaving = false;
        }
    }

    private async Task TestConnection()
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost) || string.IsNullOrWhiteSpace(_settings.SmtpUser) || string.IsNullOrWhiteSpace(_settings.SmtpPassword))
        {
            _testSuccess = false;
            _testResult = "Please fill in Host, Username, and Password before testing.";
            _testErrorDetails = null;
            return;
        }

        _isTestingConnection = true;
        _testResult = null;
        _testErrorDetails = null;
        StateHasChanged();

        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.SmtpUser, _settings.SmtpPassword)
            };

            var fromAddress = string.IsNullOrWhiteSpace(_settings.FromEmail) ? _settings.SmtpUser : _settings.FromEmail;
            var fromName = string.IsNullOrWhiteSpace(_settings.FromName) ? "AiCV Test" : _settings.FromName;

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = "AiCV - SMTP Connection Test",
                Body = "If you are reading this, your custom SMTP configuration in AiCV is working correctly!",
                IsBodyHtml = false
            };

            message.To.Add(fromAddress);

            await client.SendMailAsync(message);

            _testSuccess = true;
            _testResult = "Connection successful! A test email was sent to your from address.";
        }
        catch (Exception ex)
        {
            _testSuccess = false;
            _testResult = "Connection failed. Please check your credentials and server settings.";
            _testErrorDetails = ex.Message;
            if (ex.InnerException != null)
            {
                _testErrorDetails += " | " + ex.InnerException.Message;
            }
        }
        finally
        {
            _isTestingConnection = false;
            StateHasChanged();
        }
    }
}
