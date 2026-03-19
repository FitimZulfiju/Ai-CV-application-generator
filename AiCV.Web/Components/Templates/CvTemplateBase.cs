namespace AiCV.Web.Components.Templates;

public class CvTemplateBase : ComponentBase
{
    [Inject]
    protected IStringLocalizer<AicvResources> _localizer { get; set; } = default!;

    [Inject]
    protected IJSRuntime _jsRuntime { get; set; } = default!;

    [Parameter]
    public CandidateProfile? Profile { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Profile != null)
        {
            await Task.Delay(100);
            await _jsRuntime.InvokeVoidAsync("cvScaler.fitContentToPages");
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    protected string CalculateDuration(DateTime? start, DateTime? end, bool isCurrentRole = false)
    {
        return CvHelpers.CalculateDuration(start, end, isCurrentRole, _localizer);
    }
}
