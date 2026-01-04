namespace WebCV.Web.Components.Shared;

public partial class SampleCvDialog
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    private readonly CandidateProfile _profile = DemoProfileData.GetSampleProfile();

    private void Close() => MudDialog.Close();
}
