namespace AiCV.Web.Components.Shared;

public partial class CoverLetterPreview
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    [Parameter]
    public string LetterContent { get; set; } = string.Empty;

    [Parameter]
    public CvTemplate Template { get; set; } = CvTemplate.Professional;
}
