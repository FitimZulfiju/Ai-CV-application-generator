namespace AiCV.Web.Components.Shared;

public partial class NoteCard
{
    [Parameter, EditorRequired]
    public Note NoteItem { get; set; } = null!;

    [Parameter]
    public EventCallback<Note> OnEdit { get; set; }

    [Parameter]
    public EventCallback<Note> OnPin { get; set; }

    [Parameter]
    public EventCallback<Note> OnArchive { get; set; }

    [Parameter]
    public EventCallback<Note> OnDelete { get; set; }

    private static readonly Dictionary<string, string> NoteColors = new()
    {
        { "default", "var(--mud-palette-surface)" },
        { "red", "#f28b82" },
        { "orange", "#fbbc04" },
        { "yellow", "#fff475" },
        { "green", "#ccff90" },
        { "teal", "#a7ffeb" },
        { "blue", "#cbf0f8" },
        { "purple", "#d7aefb" },
    };

    private string GetCardStyle()
    {
        var bgColor = NoteColors.GetValueOrDefault(NoteItem.Color, "var(--mud-palette-surface)");
        var textColor = NoteItem.Color is "yellow" or "green" or "orange" ? "#202124" : "inherit";
        return $"background-color: {bgColor}; color: {textColor};";
    }

    private string GetTruncatedContent()
    {
        if (string.IsNullOrEmpty(NoteItem.Content))
            return string.Empty;

        return NoteItem.Content.Length > 300 ? NoteItem.Content[..300] + "..." : NoteItem.Content;
    }

    private async Task OnCardClick() => await OnEdit.InvokeAsync(NoteItem);
    private async Task OnPinClick() => await OnPin.InvokeAsync(NoteItem);
    private async Task OnArchiveClick() => await OnArchive.InvokeAsync(NoteItem);
    private async Task OnDeleteClick() => await OnDelete.InvokeAsync(NoteItem);
}
