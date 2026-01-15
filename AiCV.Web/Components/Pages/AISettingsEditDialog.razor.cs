namespace AiCV.Web.Components.Pages;

public partial class AISettingsEditDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public UserAIConfiguration UserConfiguration { get; set; } = new();

    [Parameter]
    public List<AIModelDto> AvailableModels { get; set; } = [];

    private bool _showApiKey = false;

    private void Cancel() => MudDialog.Cancel();

    private void Submit() => MudDialog.Close(DialogResult.Ok(UserConfiguration));

    private Task<IEnumerable<string>> SearchModels(string value, CancellationToken _)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult(AvailableModels.Select(m => m.ModelId).AsEnumerable());
        }

        var filtered = AvailableModels
            .Where(m => m.ModelId.Contains(value, StringComparison.OrdinalIgnoreCase)
                     || (m.Name?.Contains(value, StringComparison.OrdinalIgnoreCase) == true))
            .Select(m => m.ModelId);

        return Task.FromResult(filtered);
    }
}
