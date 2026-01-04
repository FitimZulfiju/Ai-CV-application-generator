namespace WebCV.Web.Components.Pages;

public partial class LogDetailsDialog
{
    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter]
    public SystemLog Log { get; set; } = default!;

    void Cancel() => MudDialog.Cancel();
}
