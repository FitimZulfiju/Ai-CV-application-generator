namespace AiCV.Web.Features.CvRendering.Templates;

public class CvTemplateBase : ComponentBase
{
    [Inject]
    protected IStringLocalizer<AicvResources> _localizer { get; set; } = default!;

    protected IStringLocalizer<AicvResources> Localizer => _localizer;

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

    protected CvRenderModel RenderModel => CvRenderModel.FromProfile(Profile, _localizer);

    protected IReadOnlyList<CvSkillGroup> SkillGroups => RenderModel.SkillGroups;

    protected IReadOnlyList<CvWorkExperienceItem> WorkExperiences => RenderModel.WorkExperiences;

    protected IReadOnlyList<CvEducationItem> OrderedEducations => RenderModel.Educations;

    protected IReadOnlyList<CvProjectItem> OrderedProjects => RenderModel.Projects;

    protected IReadOnlyList<Language> Languages => RenderModel.Languages;

    protected IReadOnlyList<Interest> Interests => RenderModel.Interests;
}
