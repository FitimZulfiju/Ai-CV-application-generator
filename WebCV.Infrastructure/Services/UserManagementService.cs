namespace WebCV.Infrastructure.Services;

/// <summary>
/// Service for user management operations
/// </summary>
public class UserManagementService(ApplicationDbContext context, UserManager<User> userManager)
    : IUserManagementService
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<User> _userManager = userManager;

    public async Task<bool> ToggleUserLockoutAsync(string userId, bool lockout)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        if (lockout)
        {
            // Lock out for 100 years (effectively permanent)
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        }
        else
        {
            // Remove lockout
            await _userManager.SetLockoutEndDateAsync(user, null);
        }

        return true;
    }

    public async Task<UserDetailDto?> GetUserDetailsAsync(string userId)
    {
        var user = await _context
            .Users.Include(u => u.CandidateProfile)
            .Include(u => u.UserSettings)
            .Include(u => u.GeneratedApplications)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        var isLockedOut = await _userManager.IsLockedOutAsync(user);

        return new UserDetailDto
        {
            Id = user.Id,
            Email = user.Email ?? "Unknown",
            FullName = user.CandidateProfile?.FullName ?? "No Profile",
            Title = user.CandidateProfile?.Title ?? "",
            Location = user.CandidateProfile?.Location ?? "",
            PhoneNumber = user.CandidateProfile?.PhoneNumber ?? "",
            EmailConfirmed = user.EmailConfirmed,
            HasProfile = user.CandidateProfile != null,
            HasApiKeys =
                user.UserSettings != null
                && (
                    !string.IsNullOrEmpty(user.UserSettings.OpenAIApiKey)
                    || !string.IsNullOrEmpty(user.UserSettings.GoogleGeminiApiKey)
                    || !string.IsNullOrEmpty(user.UserSettings.GroqApiKey)
                ),
            ApplicationsGenerated = user.GeneratedApplications?.Count ?? 0,
            Roles = [.. roles],
            RegisteredDate = DateTime.UtcNow,
            IsLockedOut = isLockedOut,
            LockoutEnd = user.LockoutEnd,
        };
    }
}
