namespace AiCV.Web.Components.Shared;

public partial class CoverLetterPreview
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    [Parameter]
    public string LetterContent { get; set; } = string.Empty;

    [Parameter]
    public CvTemplate Template { get; set; } = CvTemplate.Professional;

    private string GetTemplateClass() =>
        Template switch
        {
            CvTemplate.Modern => "cv-modern",
            CvTemplate.Minimalist => "cv-minimalist",
            _ => "cv-professional",
        };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Only scale content on first render to avoid infinite render loop
        if (firstRender && Profile != null)
        {
            // Small delay to ensure DOM is fully calculated
            await Task.Delay(100);
            await JSRuntime.InvokeVoidAsync("cvScaler.fitContentToPages");
        }
        await base.OnAfterRenderAsync(firstRender);
    }
}
