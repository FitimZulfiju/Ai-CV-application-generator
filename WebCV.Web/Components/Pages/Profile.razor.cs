namespace WebCV.Web.Components.Pages;

public partial class Profile
{
    [Inject] public ICVService CVService { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public IWebHostEnvironment Environment { get; set; } = default!;
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;

    private int _activeTabIndex;
    private CandidateProfile? _profile;

    public class SkillCategoryViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string NewSkillInput { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = new();
    }

    private List<SkillCategoryViewModel> _skillCategories = new();

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            Snackbar.Add("User not logged in.", Severity.Error);
            return;
        }

        _profile = await CVService.GetProfileAsync(userId);

        if (_profile == null)
        {
            // User likely deleted from DB but cookie persists. Force logout.
            NavigationManager.NavigateTo("/logout", true);
            return;
        }

        // Initialize categorized skills from database
        if (_profile.Skills != null && _profile.Skills.Any())
        {
            _skillCategories = [.. _profile.Skills
                .GroupBy(s => s.Category)
                .Select(g => new SkillCategoryViewModel
                {
                    Name = g.Key,
                    Skills = [.. g.Select(s => s.Name)]
                })];
        }

        // Ensure profile skills are in sync with categories initially (for defaults)
        UpdateProfileSkills();
    }

    private void AddCategory()
    {
        _skillCategories.Add(new SkillCategoryViewModel { Name = "New Category" });
        UpdateProfileSkills();
    }

    private void RemoveCategory(SkillCategoryViewModel category)
    {
        _skillCategories.Remove(category);
        UpdateProfileSkills();
    }

    private void AddSkill(SkillCategoryViewModel category)
    {
        if (!string.IsNullOrWhiteSpace(category.NewSkillInput))
        {
            var t = category.NewSkillInput.Trim();
            if (!category.Skills.Contains(t))
            {
                category.Skills.Add(t);
                UpdateProfileSkills();
            }
            category.NewSkillInput = "";
        }
    }

    private void RemoveSkill(SkillCategoryViewModel category, string skill)
    {
        category.Skills.Remove(skill);
        UpdateProfileSkills();
    }

    private void AddExperience()
    {
        _profile?.WorkExperience.Add(new Experience { StartDate = DateTime.Now });
    }

    private void RemoveExperience(Experience exp)
    {
        _profile?.WorkExperience.Remove(exp);
    }

    private void AddEducation()
    {
        _profile?.Educations.Add(new Education { StartDate = DateTime.Now });
    }

    private void RemoveEducation(Education edu)
    {
        _profile?.Educations.Remove(edu);
    }

    private void AddProject()
    {
        _profile?.Projects.Add(new Project { StartDate = DateTime.Now });
    }

    private void RemoveProject(Project proj)
    {
        _profile?.Projects.Remove(proj);
    }

    private void AddLanguage()
    {
        _profile?.Languages.Add(new Language());
    }

    private void RemoveLanguage(Language lang)
    {
        _profile?.Languages.Remove(lang);
    }

    private void AddInterest()
    {
        _profile?.Interests.Add(new Interest());
    }

    private void RemoveInterest(Interest interest)
    {
        _profile?.Interests.Remove(interest);
    }

    private void UpdateProfileSkills()
    {
        if (_profile == null) return;

        _profile.Skills.Clear();
        foreach (var category in _skillCategories)
        {
            foreach (var skillName in category.Skills)
            {
                _profile.Skills.Add(new Skill { Name = skillName, Category = category.Name });
            }
        }
    }

    private async Task SaveProfile()
    {
        if (_profile != null)
        {
            try
            {
                UpdateProfileSkills(); // Ensure it's up to date before saving
                await CVService.SaveProfileAsync(_profile);
                Snackbar.Add("Profile saved successfully!", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task PrintProfile()
    {
        await JSRuntime.InvokeVoidAsync("window.print");
    }

    private static string CalculateDuration(DateTime? start, DateTime? end)
    {
        if (!start.HasValue) return "";

        var endDate = end ?? DateTime.Now;
        var totalMonths = ((endDate.Year - start.Value.Year) * 12) + endDate.Month - start.Value.Month + 1;

        var years = totalMonths / 12;
        var months = totalMonths % 12;

        var parts = new List<string>();
        if (years > 0) parts.Add($"{years} year{(years > 1 ? "s" : "")}");
        if (months > 0) parts.Add($"{months} month{(months > 1 ? "s" : "")}");

        return string.Join(" ", parts);
    }

    private async Task UploadFiles(IBrowserFile file)
    {
        if (file == null || _profile == null) return;

        try
        {
            // Resize image to max 300x300
            var resizedFile = await file.RequestImageFileAsync(file.ContentType, 300, 300);

            // Ensure uploads directory exists for the specific user
            var uploadPath = Path.Combine(Environment.WebRootPath, "uploads", _profile.UserId);
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await resizedFile.OpenReadStream(maxAllowedSize: 1024 * 1024 * 10).CopyToAsync(stream);
            }

            var url = $"/uploads/{_profile.UserId}/{fileName}";
            _profile.ProfilePictureUrl = url;

            // Use dedicated update method to ensure persistence
            await CVService.UpdateProfilePictureAsync(_profile.Id, url);

            StateHasChanged(); // Force UI update to show the new image
            Snackbar.Add("Profile picture uploaded and saved!", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error uploading file: {ex.Message}", Severity.Error);
        }
    }

    private async Task DeleteProfilePicture()
    {
        if (_profile == null) return;

        try
        {
            if (!string.IsNullOrEmpty(_profile.ProfilePictureUrl))
            {
                var filePath = Path.Combine(Environment.WebRootPath, _profile.ProfilePictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            _profile.ProfilePictureUrl = string.Empty;
            await CVService.UpdateProfilePictureAsync(_profile.Id, string.Empty);
            StateHasChanged();
            Snackbar.Add("Profile picture removed.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error removing profile picture: {ex.Message}", Severity.Error);
        }
    }
}
