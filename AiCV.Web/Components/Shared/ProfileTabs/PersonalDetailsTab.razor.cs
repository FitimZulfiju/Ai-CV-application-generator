namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class PersonalDetailsTab
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    [Parameter]
    public EventCallback<InputFileChangeEventArgs> OnFilesChanged { get; set; }

    [Parameter]
    public EventCallback OnDeletePicture { get; set; }
}
