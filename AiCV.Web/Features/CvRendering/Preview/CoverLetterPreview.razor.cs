namespace AiCV.Web.Features.CvRendering.Preview;

public partial class CoverLetterPreview
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    [Parameter]
    public string LetterContent { get; set; } = string.Empty;

    [Parameter]
    public string Template { get; set; } = AiCV.Domain.Constants.CvTemplates.Professional;
}
