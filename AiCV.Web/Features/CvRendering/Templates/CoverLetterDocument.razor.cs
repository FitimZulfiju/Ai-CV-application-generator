namespace AiCV.Web.Features.CvRendering.Templates;

public partial class CoverLetterDocument : CvTemplateBase
{
    [Parameter]
    public string LetterContent { get; set; } = string.Empty;

    [Parameter]
    public CvThemeConfig ThemeConfig { get; set; } = ThemeRegistry.Professional;
}
