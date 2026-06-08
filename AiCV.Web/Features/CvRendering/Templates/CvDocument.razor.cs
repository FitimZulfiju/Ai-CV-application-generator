namespace AiCV.Web.Features.CvRendering.Templates;

public partial class CvDocument : CvTemplateBase
{
    [Parameter]
    public CvThemeConfig ThemeConfig { get; set; } = ThemeRegistry.Professional;
}
