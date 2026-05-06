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

    /// <summary>
    /// Called by AiModelPicker after a model is selected — updates the
    /// cost type and notes from the selected model's metadata.
    /// </summary>
    private Task OnModelSelectedCallback(string? modelId)
    {
        var selectedModel = AvailableModels.FirstOrDefault(m => m.ModelId == modelId);
        if (selectedModel != null)
        {
            UserConfiguration.CostType = selectedModel.CostType;
            UserConfiguration.Notes = selectedModel.Notes.Count != 0
                ? string.Join(", ", selectedModel.Notes)
                : null;
        }
        return Task.CompletedTask;
    }
}
