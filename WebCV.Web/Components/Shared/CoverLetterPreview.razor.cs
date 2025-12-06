namespace WebCV.Web.Components.Shared;

public partial class CoverLetterPreview
{
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    [Parameter] public CandidateProfile? Profile { get; set; }
    [Parameter] public string LetterContent { get; set; } = string.Empty;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Profile != null)
        {
             // Small delay to ensure DOM is fully calculated
            await Task.Delay(100);
            await JSRuntime.InvokeVoidAsync("cvScaler.fitContentToPages");
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private string FormatLetter(string content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        // Basic formatting if needed, but pre-wrap handles most
        return content;
    }
}
