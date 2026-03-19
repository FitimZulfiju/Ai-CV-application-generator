namespace AiCV.Web.Components.Pages;

public partial class Profile
{
    private PrintPreviewModal _printPreviewModal = default!;

    private int _activeTabIndex;
    private bool _showFormattingHelp;
    private bool _isSaving;
    private bool _isPrinting;
    private CvTemplate _selectedTemplate = CvTemplate.Professional;
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
            Navigation.NavigateTo($"/{NavUri.LogoutPage}", true);
            return;
        }

        if (_profile.Skills != null && _profile.Skills.Count != 0)
        {
            _skillCategories =
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
                UpdateProfileSkills();
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
