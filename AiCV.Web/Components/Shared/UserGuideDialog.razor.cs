namespace AiCV.Web.Components.Shared;

public partial class UserGuideDialog
{
    [Inject]
    private IStringLocalizer<AicvResources> Localizer { get; set; } = default!;

    [CascadingParameter]
    protected IMudDialogInstance MudDialog { get; set; } = default!;

    protected void Close() => MudDialog.Close();

    protected record FormattingExample(string Description, string Code, string Preview);

    protected List<FormattingExample> _formattingExamples =>
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
