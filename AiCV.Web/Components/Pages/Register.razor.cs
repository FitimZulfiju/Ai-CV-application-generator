namespace AiCV.Web.Components.Pages;

public partial class Register
{
    [SupplyParameterFromQuery]
    public string? Error { get; set; }

    private string _email = "";
    private string _password = "";
    private string _confirmPassword = "";
    private bool _isProcessing = false;

    private async Task HandleRegister()
    {
        if (_password != _confirmPassword)
        {
            Error = "Passwords do not match";
            return;
        }

        _isProcessing = true;
        Error = null;

        try
        {
            // Check if user already exists
            var userByEmail = await UserManager.FindByEmailAsync(_email);
            var userByName = await UserManager.FindByNameAsync(_email);

            if (userByEmail != null || userByName != null)
            {
                Error = $"UserName or Email '{_email}' is already taken.";
                _isProcessing = false;
                return;
            }

            var user = new User { UserName = _email, Email = _email };
            var result = await UserManager.CreateAsync(user, _password);

            if (result.Succeeded)
            {
                await UserManager.AddToRoleAsync(user, Roles.User);

                // Create profile
                var profile = new CandidateProfile
                {
                    UserId = user.Id,
                    FullName = _email.Split('@')[0],
                    Email = _email,
                    Title = "Candidate",
                    PhoneNumber = "",
                    LinkedInUrl = "",
                    PortfolioUrl = "",
                    Location = "",
                    ProfessionalSummary = "",
                    ProfilePictureUrl = "",
                    Tagline = "",
                    Skills = [],
                    WorkExperience = [],
                    Educations = [],
                };

                await using var context = await DbContextFactory.CreateDbContextAsync();
                context.CandidateProfiles.Add(profile);
                await context.SaveChangesAsync();

                // To perform sign-in, we must redirect to a static endpoint
                // because Blazor Server components cannot set auth cookies directly.
                Navigation.NavigateTo(
                    $"/perform-login?email={Uri.EscapeDataString(_email)}&password={Uri.EscapeDataString(_password)}",
                    true
                );
            }
            else
            {
                Error = string.Join(", ", result.Errors.Select(e => e.Description));
                _isProcessing = false;
            }
        }
        catch (Exception ex)
        {
            if (ex.ToString().Contains("duplicate") || ex.ToString().Contains("Index"))
            {
                Error = $"UserName or Email '{_email}' is already taken.";
            }
            else
            {
                Error = "An unexpected error occurred. Please try again.";
            }
            _isProcessing = false;
        }
    }
}
