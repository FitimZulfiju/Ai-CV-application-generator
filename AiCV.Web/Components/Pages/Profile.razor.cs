namespace AiCV.Web.Components.Pages;

public partial class Profile : IDisposable
{
    private Timer? _autoSaveTimer;

    private PrintPreviewModal _printPreviewModal = default!;

    private int _activeTabIndex;
    private bool _showFormattingHelp;
    private bool _isSaving;
    private bool _isPrinting;
    private CandidateProfile? _profile;

    public class SkillCategoryViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string NewSkillInput { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = [];
    }

    private List<SkillCategoryViewModel> _skillCategories = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            Snackbar.Add("User not logged in.", Severity.Error);
            return;
        }

        _profile = await CVService.GetProfileAsync(userId);

        if (_profile == null)
        {
            // User likely deleted from DB but cookie persists. Force logout.
            Navigation.NavigateTo($"/{NavUri.LogoutPage}", true);
            return;
        }

        // Initialize categorized skills from database
        if (_profile.Skills != null && _profile.Skills.Count != 0)
        {
            _skillCategories =
            [
                .. _profile
                    .Skills.GroupBy(s => s.Category ?? "Uncategorized")
                    .Select(g => new SkillCategoryViewModel
                    {
                        Name = g.Key,
                        // Use Distinct to prevent any duplicates
                        Skills = [.. g.Select(s => s.Name).Distinct()],
                    }),
            ];
        }

        // Auto-migrate SectionTitle to SectionDescription for backward compatibility
        if (_profile.Projects != null)
        {
            foreach (var proj in _profile.Projects)
            {
                if (
                    string.IsNullOrEmpty(proj.SectionDescription)
                    && !string.IsNullOrEmpty(proj.SectionTitle)
                    && proj.SectionTitle.Contains('\n')
                )
                {
                    var lines = proj
                        .SectionTitle.Replace("\r\n", "\n")
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 1)
                    {
                        proj.SectionTitle = lines[0].Trim();
                        proj.SectionDescription = string.Join("\n", lines.Skip(1));
                    }
                }
            }
        }
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
        if (_profile == null)
            return;

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
            _isSaving = true;
            StateHasChanged();
            await Task.Yield();
            try
            {
                UpdateProfileSkills(); // Ensure it's up to date before saving
                await CVService.SaveProfileAsync(_profile);
                await PersistenceService.ClearDraftAsync("profile_draft");
                Snackbar.Add("Profile saved successfully!", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
            finally
            {
                _isSaving = false;
                StateHasChanged();
            }
        }
    }

    private async Task ClearDraft()
    {
        var result = await DialogService.ShowMessageBox(
            Localizer["ClearDraftConfirmationTitle"],
            Localizer["ClearDraftConfirmationContent"],
            yesText: Localizer["ClearDraftConfirmButton"],
            cancelText: Localizer["ClearDraftCancelButton"]
        );

        if (result == true)
        {
            await PersistenceService.ClearDraftAsync("profile_draft");
            await LoadProfileAsync();
            Snackbar.Add(Localizer["DraftCleared"], Severity.Info);
        }
    }

    private async Task PrintProfile()
    {
        if (_profile == null)
            return;

        _isPrinting = true;
        StateHasChanged();
        await Task.Yield();
        try
        {
            var pdfBytes = await PdfService.GenerateCvAsync(_profile);
            await _printPreviewModal.ShowAsync(pdfBytes, "CV", _profile.FullName);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error generating PDF: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isPrinting = false;
            StateHasChanged();
        }
    }

    private string CalculateDuration(DateTime? start, DateTime? end)
    {
        if (!start.HasValue)
            return "";

        var endDate = end ?? DateTime.Now;
        var totalMonths =
            ((endDate.Year - start.Value.Year) * 12) + endDate.Month - start.Value.Month + 1;

        var years = totalMonths / 12;
        var months = totalMonths % 12;

        var parts = new List<string>();
        if (years > 0)
        {
            var yearKey = years > 1 ? "Years" : "Year";
            parts.Add($"{years} {Localizer[yearKey]}");
        }
        if (months > 0)
        {
            var monthKey = months > 1 ? "Months" : "Month";
            parts.Add($"{months} {Localizer[monthKey]}");
        }

        return string.Join(" ", parts);
    }

    private async Task UploadFiles(IBrowserFile file)
    {
        if (file == null || _profile == null)
            return;

        try
        {
            // Resize image to max 400x400 (approx 3cm at 300dpi is 354px)
            var resizedFile = await file.RequestImageFileAsync(file.ContentType, 400, 400);

            // Ensure uploads directory exists for the specific user
            var uploadPath = Path.Combine(Environment.WebRootPath, "uploads", _profile.UserId);
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
            var filePath = Path.Combine(uploadPath, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await resizedFile
                    .OpenReadStream(maxAllowedSize: 1024 * 1024 * 10)
                    .CopyToAsync(stream);
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
        if (_profile == null)
            return;

        try
        {
            if (!string.IsNullOrEmpty(_profile.ProfilePictureUrl))
            {
                var filePath = Path.Combine(
                    Environment.WebRootPath,
                    _profile
                        .ProfilePictureUrl.TrimStart('/')
                        .Replace('/', Path.DirectorySeparatorChar)
                );
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

    // Track previous state hash or simply save periodically if _profile is not null

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Try to restore draft
            var draft = await PersistenceService.GetDraftAsync<CandidateProfile>("profile_draft");
            if (draft != null && _profile != null)
            {
                // Simple Restoration Strategy:
                // If the server profile is "empty" (new) OR we want to prioritize draft.
                // Given the requirement is "don't lose work on crash", we assume the draft is more recent/valuable
                // if it exists.

                // We map draft to _profile
                _profile = draft;

                // Re-initialize view models
                if (_profile.Skills != null)
                {
                    _skillCategories =
                    [
                        .. _profile
                            .Skills.GroupBy(s => s.Category ?? "Uncategorized")
                            .Select(g => new SkillCategoryViewModel
                            {
                                Name = g.Key,
                                // Use Distinct to prevent any duplicates from draft
                                Skills = [.. g.Select(s => s.Name).Distinct()],
                            }),
                    ];
                }

                StateHasChanged();
                Snackbar.Add("Draft restored from browser storage.", Severity.Info);
            }

            // Start Auto-Save Timer (every 5 seconds)
            _autoSaveTimer = new Timer(
                async _ =>
                {
                    if (_profile != null)
                    {
                        await InvokeAsync(async () =>
                        {
                            // Sync ViewModels to Profile before saving
                            UpdateProfileSkills();
                            await PersistenceService.SaveDraftAsync("profile_draft", _profile);
                        });
                    }
                },
                null,
                5000,
                5000
            );
        }
    }

    public void Dispose()
    {
        _autoSaveTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
