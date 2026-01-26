namespace AiCV.Web.Components.Pages;

public partial class NoteDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public Note NoteModel { get; set; } = new();

    private static readonly Dictionary<string, string> NoteColors = new()
    {
        { "default", "transparent" },
        { "red", "#f28b82" },
        { "orange", "#fbbc04" },
        { "yellow", "#fff475" },
        { "green", "#ccff90" },
        { "teal", "#a7ffeb" },
        { "blue", "#cbf0f8" },
        { "purple", "#d7aefb" },
    };

    private static string GetColorStyle(string colorKey)
    {
        var bgColor = NoteColors.GetValueOrDefault(colorKey, "transparent");
        if (colorKey == "default")
        {
            return "border: 2px solid var(--mud-palette-lines-default);";
        }
        return $"background-color: {bgColor}; border: 2px solid {bgColor};";
    }

    private void Submit() => MudDialog.Close(DialogResult.Ok(NoteModel));
    private void Cancel() => MudDialog.Cancel();
}
