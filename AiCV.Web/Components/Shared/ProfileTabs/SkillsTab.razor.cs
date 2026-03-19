namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class SkillsTab
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    [Parameter]
    public List<Profile.SkillCategoryViewModel> SkillCategories { get; set; } = [];

    [Parameter]
    public EventCallback OnSkillsUpdated { get; set; }

    private async Task AddCategory()
    {
        SkillCategories.Add(new Profile.SkillCategoryViewModel { Name = "New Category" });
        await OnSkillsUpdated.InvokeAsync();
    }

    private async Task RemoveCategory(Profile.SkillCategoryViewModel category)
    {
        SkillCategories.Remove(category);
        await OnSkillsUpdated.InvokeAsync();
    }

    private async Task AddSkill(Profile.SkillCategoryViewModel category)
    {
        if (!string.IsNullOrWhiteSpace(category.NewSkillInput))
        {
            var t = category.NewSkillInput.Trim();
            if (!category.Skills.Contains(t))
            {
                category.Skills.Add(t);
                await OnSkillsUpdated.InvokeAsync();
            }
            category.NewSkillInput = "";
        }
    }

    private async Task RemoveSkill(Profile.SkillCategoryViewModel category, string skill)
    {
        category.Skills.Remove(skill);
        await OnSkillsUpdated.InvokeAsync();
    }
}
