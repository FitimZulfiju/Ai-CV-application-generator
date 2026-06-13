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
        RefreshUI();
        await OnSkillsUpdated.InvokeAsync();
    }

    private async Task RemoveCategory(Profile.SkillCategoryViewModel category)
    {
        SkillCategories.Remove(category);
        RefreshUI();
        await OnSkillsUpdated.InvokeAsync();
    }

    private string? _editingSkillOriginalValue = null;
    private Profile.SkillCategoryViewModel? _editingSkillCategory = null;
    private MudDropContainer<Profile.SkillCategoryViewModel>? _categoryDropContainer;
    private Dictionary<Profile.SkillCategoryViewModel, MudDropContainer<string>> _skillContainers = new();

    private void RefreshUI()
    {
        _categoryDropContainer?.Refresh();
        foreach (var container in _skillContainers.Values)
        {
            container?.Refresh();
        }
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
            await OnSkillsUpdated.InvokeAsync();
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
        await OnSkillsUpdated.InvokeAsync();
    }

    private void OnSkillDropped(Profile.SkillCategoryViewModel category, MudItemDropInfo<string> dropInfo)
    {
        if (dropInfo.Item == null) return;
        var skill = dropInfo.Item;
        category.Skills.Remove(skill);
        var newIndex = Math.Min(dropInfo.IndexInZone, category.Skills.Count);
        category.Skills.Insert(newIndex, skill);
        category.Skills = category.Skills.ToList();
        RefreshUI();
        _ = OnSkillsUpdated.InvokeAsync();
    }

    private void OnCategoryDropped(MudItemDropInfo<Profile.SkillCategoryViewModel> dropInfo)
    {
        if (dropInfo.Item == null) return;
        var category = dropInfo.Item;
        SkillCategories.Remove(category);
        var newIndex = Math.Min(dropInfo.IndexInZone, SkillCategories.Count);
        SkillCategories.Insert(newIndex, category);
        _ = OnSkillsUpdated.InvokeAsync();
    }
}
