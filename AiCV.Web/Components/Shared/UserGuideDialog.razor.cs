namespace AiCV.Web.Components.Shared;

public partial class UserGuideDialog
{
    [CascadingParameter]
    protected IMudDialogInstance MudDialog { get; set; } = default!;

    protected void Close() => MudDialog.Close();

    protected record FormattingExample(string Description, string Code, string Preview);

    protected readonly List<FormattingExample> _formattingExamples =
    [
        new("Bold Text", "<b>Leader</b>", "<b>Leader</b>"),
        new("Italic Text", "<i>Innovative</i>", "<i>Innovative</i>"),
        new(
            "Colored Text",
            "<span style=\"color:blue\">Blue Text</span>",
            "<span style=\"color:blue\">Blue Text</span>"
        ),
        new("Bullet List", "• Item 1<br>• Item 2", "• Item 1<br>• Item 2"),
        new("Line Break", "Line 1<br>Line 2", "Line 1<br>Line 2"),
    ];
}
