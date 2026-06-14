namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class CoreCompetenciesTab
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    [Parameter]
    public List<Profile.SkillCategoryViewModel> CoreCompetencyCategories { get; set; } = [];

    [Parameter]
    public EventCallback OnCoreCompetenciesUpdated { get; set; }

    private async Task AddCategory()
    {
        CoreCompetencyCategories.Add(new Profile.SkillCategoryViewModel { Name = "New Category" });
        RefreshUI();
        await OnCoreCompetenciesUpdated.InvokeAsync();
    }

    private async Task RemoveCategory(Profile.SkillCategoryViewModel category)
    {
        CoreCompetencyCategories.Remove(category);
        RefreshUI();
        await OnCoreCompetenciesUpdated.InvokeAsync();
    }

    private string? _editingSkillOriginalValue = null;
    private Profile.SkillCategoryViewModel? _editingSkillCategory = null;

    private void RefreshUI()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task AddSkill(Profile.SkillCategoryViewModel category)
    {
        if (!string.IsNullOrWhiteSpace(category.NewSkillInput))
        {
            var t = category.NewSkillInput.Trim();
            
            if (_editingSkillCategory == category && _editingSkillOriginalValue != null)
            {
                var index = category.Skills.IndexOf(_editingSkillOriginalValue);
                if (index != -1)
                {
                    category.Skills[index] = t;
                }
                else if (!category.Skills.Contains(t))
                {
                    category.Skills.Add(t);
                }
                _editingSkillCategory = null;
                _editingSkillOriginalValue = null;
            }
            else
            {
                if (!category.Skills.Contains(t))
                {
                    category.Skills.Add(t);
                }
            }
            category.Skills = category.Skills.ToList();
            category.NewSkillInput = "";
            RefreshUI();
            await OnCoreCompetenciesUpdated.InvokeAsync();
        }
    }

    private void BeginSkillEdit(Profile.SkillCategoryViewModel category, string skill)
    {
        _editingSkillCategory = category;
        _editingSkillOriginalValue = skill;
        category.NewSkillInput = skill;
        RefreshUI();
    }

    private void CancelSkillEdit(Profile.SkillCategoryViewModel category)
    {
        _editingSkillCategory = null;
        _editingSkillOriginalValue = null;
        category.NewSkillInput = "";
        RefreshUI();
    }

    private async Task RemoveSkill(Profile.SkillCategoryViewModel category, string skill)
    {
        if (_editingSkillOriginalValue == skill && _editingSkillCategory == category)
        {
            CancelSkillEdit(category);
        }

        category.Skills.Remove(skill);
        category.Skills = category.Skills.ToList();
        RefreshUI();
        await OnCoreCompetenciesUpdated.InvokeAsync();
    }

    private async Task MoveCategoryUp(Profile.SkillCategoryViewModel category)
    {
        var index = CoreCompetencyCategories.IndexOf(category);
        if (index > 0)
        {
            CoreCompetencyCategories.RemoveAt(index);
            CoreCompetencyCategories.Insert(index - 1, category);
            RefreshUI();
            await OnCoreCompetenciesUpdated.InvokeAsync();
        }
    }

    private async Task MoveCategoryDown(Profile.SkillCategoryViewModel category)
    {
        var index = CoreCompetencyCategories.IndexOf(category);
        if (index < CoreCompetencyCategories.Count - 1)
        {
            CoreCompetencyCategories.RemoveAt(index);
            CoreCompetencyCategories.Insert(index + 1, category);
            RefreshUI();
            await OnCoreCompetenciesUpdated.InvokeAsync();
        }
    }
}
