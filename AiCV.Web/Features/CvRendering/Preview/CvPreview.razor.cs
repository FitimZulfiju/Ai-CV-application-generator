namespace AiCV.Web.Features.CvRendering.Preview;

public partial class CvPreview
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    [Parameter]
    public string Template { get; set; } = AiCV.Domain.Constants.CvTemplates.Professional;
}
