namespace AiCV.Web.Components.Pages;

public partial class Profile
{
    private const long MaxProfileImportFileBytes = 1024 * 1024 * 5;
    private const int MaxProfileTextLength = 20_000;
    private const int MaxProfileCollectionItems = 500;

    private PrintPreviewModal _printPreviewModal = default!;
    private static readonly JsonSerializerOptions ProfileJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private int _activeTabIndex;
    private bool _showFormattingHelp;
    private bool _isLoading = true;
    private bool _isSaving;
    private bool _isPrinting;
    private string _selectedTemplate = AiCV.Domain.Constants.CvTemplates.Professional;
    private CandidateProfile? _profile;

    public class SkillCategoryViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string NewSkillInput { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = [];
    }

    private List<SkillCategoryViewModel> _coreCompetenciesCategories = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        _isLoading = true;
        LoadingService.Show("Loading profile...", 0);
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        try
        {
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
                Navigation.NavigateTo($"/{NavUri.LogoutPage}", true);
                return;
            }

            if (_profile.Skills != null && _profile.Skills.Count != 0)
            {
                _coreCompetenciesCategories =
                [
                    .. _profile
                        .Skills.GroupBy(s => s.Category ?? "Uncategorized")
                        .Select(g => new SkillCategoryViewModel
                        {
                            Name = g.Key,
                            Skills = [.. g.Select(s => s.Name).Distinct()],
                        }),
                ];
            }

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
        finally
        {
            _isLoading = false;
            LoadingService.Hide();
        }
    }

    private void UpdateProfileCoreCompetencies()
    {
        if (_profile == null)
            return;

        _profile.Skills.Clear();
        foreach (var category in _coreCompetenciesCategories)
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
                UpdateProfileCoreCompetencies();
                await CVService.SaveProfileAsync(_profile);
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

    private async Task ExportProfile()
    {
        if (_profile == null)
        {
            return;
        }

        UpdateProfileCoreCompetencies();

        var exportProfile = CloneProfileForExport(_profile);
        var json = JsonSerializer.Serialize(exportProfile, ProfileJsonOptions);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var fileName = $"profile-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";

        await using var stream = new MemoryStream(bytes);
        using var streamReference = new DotNetStreamReference(stream);
        await JSRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamReference);

        Snackbar.Add(Localizer["ProfileExported"], Severity.Success);
    }

    private async Task ImportProfile(InputFileChangeEventArgs fileChange)
    {
        if (_profile == null || fileChange == null)
        {
            return;
        }

        var file = fileChange.File;

        var confirmed = await DialogService.ShowMessageBoxAsync(
            Localizer["ImportProfileWarningTitle"],
            Localizer["ImportProfileWarningContent"],
            yesText: Localizer["ImportProfile"],
            cancelText: Localizer["Cancel"]
        );

        if (confirmed != true)
        {
            return;
        }

        try
        {
            if (!IsJsonFile(file))
            {
                Snackbar.Add(Localizer["InvalidProfileJson"], Severity.Error);
                return;
            }

            await using var stream = file.OpenReadStream(maxAllowedSize: MaxProfileImportFileBytes);
            var importedProfile = await JsonSerializer.DeserializeAsync<CandidateProfile>(
                stream,
                ProfileJsonOptions
            );

            if (importedProfile == null)
            {
                Snackbar.Add(Localizer["InvalidProfileJson"], Severity.Error);
                return;
            }

            EnsureProfileCollections(importedProfile);
            if (!IsValidImportedProfile(importedProfile))
            {
                Snackbar.Add(Localizer["InvalidProfileJson"], Severity.Error);
                return;
            }

            NormalizeImportedProfile(importedProfile, _profile);
            await CVService.SaveProfileAsync(importedProfile);

            _profile = importedProfile;
            RefreshCoreCompetencyCategoriesFromProfile();
            Snackbar.Add(Localizer["ProfileImported"], Severity.Success);
            StateHasChanged();
        }
        catch (JsonException)
        {
            Snackbar.Add(Localizer["InvalidProfileJson"], Severity.Error);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"{Localizer["ImportProfileFailed"]}: {ex.Message}", Severity.Error);
        }
    }

    private static bool IsJsonFile(IBrowserFile file)
    {
        return file.Size is > 0 and <= MaxProfileImportFileBytes
            && (
                file.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(file.ContentType, "application/json", StringComparison.OrdinalIgnoreCase)
            );
    }

    private static bool IsValidImportedProfile(CandidateProfile profile)
    {
        if (!HasAnyProfileContent(profile))
        {
            return false;
        }

        return HasValidProfileTextLengths(profile)
            && profile.WorkExperience.Count <= MaxProfileCollectionItems
            && profile.Educations.Count <= MaxProfileCollectionItems
            && profile.Skills.Count <= MaxProfileCollectionItems
            && profile.Projects.Count <= MaxProfileCollectionItems
            && profile.Languages.Count <= MaxProfileCollectionItems
            && profile.Interests.Count <= MaxProfileCollectionItems
            && profile.WorkExperience.All(HasValidExperienceTextLengths)
            && profile.Educations.All(HasValidEducationTextLengths)
            && profile.Skills.All(HasValidSkillTextLengths)
            && profile.Projects.All(HasValidProjectTextLengths)
            && profile.Languages.All(HasValidLanguageTextLengths)
            && profile.Interests.All(HasValidInterestTextLengths);
    }

    private static bool HasAnyProfileContent(CandidateProfile profile)
    {
        return !string.IsNullOrWhiteSpace(profile.FullName)
            || !string.IsNullOrWhiteSpace(profile.Email)
            || !string.IsNullOrWhiteSpace(profile.Title)
            || !string.IsNullOrWhiteSpace(profile.ProfessionalSummary)
            || profile.WorkExperience.Count != 0
            || profile.Educations.Count != 0
            || profile.Skills.Count != 0
            || profile.Projects.Count != 0
            || profile.Languages.Count != 0
            || profile.Interests.Count != 0;
    }

    private static bool HasValidProfileTextLengths(CandidateProfile profile)
    {
        return IsValidImportText(profile.UserId)
            && IsValidImportText(profile.FullName)
            && IsValidImportText(profile.Title)
            && IsValidImportText(profile.Email)
            && IsValidImportText(profile.PhoneNumber)
            && IsValidImportText(profile.LinkedInUrl)
            && IsValidImportText(profile.PortfolioUrl)
            && IsValidImportText(profile.Location)
            && IsValidImportText(profile.ProfessionalSummary)
            && IsValidImportText(profile.ProfilePictureUrl)
            && IsValidImportText(profile.Tagline);
    }

    private static bool HasValidExperienceTextLengths(Experience experience)
    {
        return IsValidImportText(experience.CompanyName)
            && IsValidImportText(experience.JobTitle)
            && IsValidImportText(experience.Description)
            && IsValidImportText(experience.Location);
    }

    private static bool HasValidEducationTextLengths(Education education)
    {
        return IsValidImportText(education.InstitutionName)
            && IsValidImportText(education.Degree)
            && IsValidImportText(education.Description);
    }

    private static bool HasValidSkillTextLengths(Skill skill)
    {
        return IsValidImportText(skill.Name) && IsValidImportText(skill.Category);
    }

    private static bool HasValidProjectTextLengths(Project project)
    {
        return IsValidImportText(project.Name)
            && IsValidImportText(project.Role)
            && IsValidImportText(project.Description)
            && IsValidImportText(project.SectionTitle)
            && IsValidImportText(project.SectionDescription)
            && IsValidImportText(project.Technologies)
            && IsValidImportText(project.Link);
    }

    private static bool HasValidLanguageTextLengths(Language language)
    {
        return IsValidImportText(language.Name) && IsValidImportText(language.Proficiency);
    }

    private static bool HasValidInterestTextLengths(Interest interest)
    {
        return IsValidImportText(interest.Name);
    }

    private static bool IsValidImportText(string? value)
    {
        return value == null || value.Length <= MaxProfileTextLength;
    }

    private static CandidateProfile CloneProfileForExport(CandidateProfile profile)
    {
        var json = JsonSerializer.Serialize(profile, ProfileJsonOptions);
        var exportProfile = JsonSerializer.Deserialize<CandidateProfile>(json, ProfileJsonOptions)
            ?? new CandidateProfile();

        exportProfile.Id = 0;
        exportProfile.UserId = string.Empty;
        exportProfile.User = null;

        foreach (var skill in exportProfile.Skills)
        {
            skill.Id = 0;
            skill.CandidateProfileId = 0;
            skill.CandidateProfile = null;
        }

        foreach (var experience in exportProfile.WorkExperience)
        {
            experience.Id = 0;
            experience.CandidateProfileId = 0;
            experience.CandidateProfile = null;
        }

        foreach (var education in exportProfile.Educations)
        {
            education.Id = 0;
            education.CandidateProfileId = 0;
            education.CandidateProfile = null;
        }

        foreach (var project in exportProfile.Projects)
        {
            project.Id = 0;
            project.CandidateProfileId = 0;
            project.CandidateProfile = null;
        }

        foreach (var language in exportProfile.Languages)
        {
            language.Id = 0;
            language.CandidateProfileId = 0;
            language.CandidateProfile = null;
        }

        foreach (var interest in exportProfile.Interests)
        {
            interest.Id = 0;
            interest.CandidateProfileId = 0;
            interest.CandidateProfile = null;
        }

        return exportProfile;
    }

    private static void NormalizeImportedProfile(CandidateProfile importedProfile, CandidateProfile currentProfile)
    {
        EnsureProfileCollections(importedProfile);

        importedProfile.Id = currentProfile.Id;
        importedProfile.UserId = currentProfile.UserId;
        importedProfile.User = null;
        importedProfile.FullName ??= string.Empty;
        importedProfile.Title ??= string.Empty;
        importedProfile.Email ??= string.Empty;
        importedProfile.PhoneNumber ??= string.Empty;
        importedProfile.LinkedInUrl ??= string.Empty;
        importedProfile.PortfolioUrl ??= string.Empty;
        importedProfile.Location ??= string.Empty;
        importedProfile.ProfessionalSummary ??= string.Empty;
        importedProfile.ProfilePictureUrl ??= string.Empty;
        importedProfile.Tagline ??= string.Empty;

        foreach (var skill in importedProfile.Skills)
        {
            skill.Id = 0;
            skill.CandidateProfileId = currentProfile.Id;
            skill.CandidateProfile = null;
        }

        foreach (var experience in importedProfile.WorkExperience)
        {
            experience.Id = 0;
            experience.CandidateProfileId = currentProfile.Id;
            experience.CandidateProfile = null;
        }

        foreach (var education in importedProfile.Educations)
        {
            education.Id = 0;
            education.CandidateProfileId = currentProfile.Id;
            education.CandidateProfile = null;
        }

        foreach (var project in importedProfile.Projects)
        {
            project.Id = 0;
            project.CandidateProfileId = currentProfile.Id;
            project.CandidateProfile = null;
        }

        foreach (var language in importedProfile.Languages)
        {
            language.Id = 0;
            language.CandidateProfileId = currentProfile.Id;
            language.CandidateProfile = null;
        }

        foreach (var interest in importedProfile.Interests)
        {
            interest.Id = 0;
            interest.CandidateProfileId = currentProfile.Id;
            interest.CandidateProfile = null;
        }
    }

    private static void EnsureProfileCollections(CandidateProfile profile)
    {
        profile.WorkExperience ??= [];
        profile.Educations ??= [];
        profile.Skills ??= [];
        profile.Projects ??= [];
        profile.Languages ??= [];
        profile.Interests ??= [];
    }

    private void RefreshCoreCompetencyCategoriesFromProfile()
    {
        _coreCompetenciesCategories = [];

        if (_profile?.Skills == null || _profile.Skills.Count == 0)
        {
            return;
        }

        _coreCompetenciesCategories =
        [
            .. _profile
                .Skills.GroupBy(s => s.Category ?? "Uncategorized")
                .Select(g => new SkillCategoryViewModel
                {
                    Name = g.Key,
                    Skills = [.. g.Select(s => s.Name).Distinct()],
                }),
        ];
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
            var pdfBytes = await PdfService.GenerateCvAsync(_profile, _selectedTemplate);
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

    private async Task UploadFiles(InputFileChangeEventArgs file)
    {
        if (file == null || _profile == null)
            return;

        try
        {
            var resizedFile = await file.File.RequestImageFileAsync(
                file.File.ContentType,
                400,
                400
            );

            var webRootPath =
                Environment.WebRootPath ?? Path.Combine(Environment.ContentRootPath, "wwwroot");
            var uploadPath = Path.Combine(webRootPath, "uploads", _profile.UserId);
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.File.Name)}";
            var filePath = Path.Combine(uploadPath, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await resizedFile
                    .OpenReadStream(maxAllowedSize: 1024 * 1024 * 10)
                    .CopyToAsync(stream);
            }

            var url = $"/uploads/{_profile.UserId}/{fileName}";
            _profile.ProfilePictureUrl = url;

            await CVService.UpdateProfilePictureAsync(_profile.Id, url);

            StateHasChanged();
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
                var webRootPath =
                    Environment.WebRootPath ?? Path.Combine(Environment.ContentRootPath, "wwwroot");
                var filePath = Path.Combine(
                    webRootPath,
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) { }
    }
}
