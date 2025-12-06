namespace WebCV.Web.Components.Pages;

public partial class UserSettingsPage
{
    [Inject] public IUserSettingsService UserSettingsService { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    private SettingsModel _model = new();
    private bool _isLoading = false;
    private string _userId = string.Empty;

    private bool _showOpenAiKey = false;
    private bool _showGoogleKey = false;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            _userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (!string.IsNullOrEmpty(_userId))
            {
                await LoadSettings();
            }
        }
    }

    private async Task LoadSettings()
    {
        _isLoading = true;
        try
        {
            var settings = await UserSettingsService.GetUserSettingsAsync(_userId);
            if (settings != null)
            {
                _model.OpenAIApiKey = settings.OpenAIApiKey ?? string.Empty;
                _model.GoogleGeminiApiKey = settings.GoogleGeminiApiKey ?? string.Empty;
                _model.DefaultModel = settings.DefaultModel;
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading settings: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task SaveSettings()
    {
        _isLoading = true;
        try
        {
            await UserSettingsService.SaveUserSettingsAsync(_userId, _model.OpenAIApiKey, _model.GoogleGeminiApiKey, _model.DefaultModel);
            Snackbar.Add("Settings saved successfully!", Severity.Success);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("User not found") || (ex.InnerException != null && ex.InnerException.Message.Contains("User not found")))
            {
                 NavigationManager.NavigateTo("/logout", true);
                 return;
            }

            var message = ex.InnerException?.Message ?? ex.Message;
            Snackbar.Add($"Error saving settings: {message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void ToggleOpenAiVisibility()
    {
        _showOpenAiKey = !_showOpenAiKey;
    }

    private void ToggleGoogleVisibility()
    {
        _showGoogleKey = !_showGoogleKey;
    }

    private class SettingsModel
    {
        public string OpenAIApiKey { get; set; } = string.Empty;
        public string GoogleGeminiApiKey { get; set; } = string.Empty;
        public AIModel DefaultModel { get; set; } = AIModel.Gpt4o;
    }
}
