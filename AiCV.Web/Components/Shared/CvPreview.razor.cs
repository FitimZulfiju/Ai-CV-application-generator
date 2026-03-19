namespace AiCV.Web.Components.Shared;

public partial class CvPreview
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    [Parameter]
    public CvTemplate Template { get; set; } = CvTemplate.Professional;
}
