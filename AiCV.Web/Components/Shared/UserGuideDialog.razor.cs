namespace AiCV.Web.Components.Shared;

public partial class UserGuideDialog
{
    [CascadingParameter]
    protected IMudDialogInstance MudDialog { get; set; } = default!;

    protected void Close() => MudDialog.Close();

    protected record FormattingExample(string Description, string Code, string Preview);

    private List<FormattingExample> FormattingExamples =>
        [
            new(Localizer["BoldText"], "<b>Leader</b>", "<b>Leader</b>"),
            new(Localizer["ItalicText"], "<i>Innovative</i>", "<i>Innovative</i>"),
            new(
                Localizer["ColoredText"],
                "<span style=\"color:blue\">Blue Text</span>",
                "<span style=\"color:blue\">Blue Text</span>"
            ),
            new(Localizer["BulletList"], "• Item 1<br>• Item 2", "• Item 1<br>• Item 2"),
            new(Localizer["LineBreak"], "Line 1<br>Line 2", "Line 1<br>Line 2"),
        ];
}
