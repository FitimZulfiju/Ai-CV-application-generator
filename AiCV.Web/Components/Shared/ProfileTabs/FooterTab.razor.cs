namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class FooterTab : ComponentBase
{
    [Parameter, EditorRequired]
    public CandidateProfile Profile { get; set; } = default!;
}
