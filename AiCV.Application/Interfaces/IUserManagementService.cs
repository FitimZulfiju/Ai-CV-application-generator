namespace AiCV.Application.Interfaces;

/// <summary>
/// Service interface for user management operations
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Toggle user account lockout status
    /// </summary>
    Task<bool> ToggleUserLockoutAsync(string userId, bool lockout);

    /// <summary>
    /// Get user details by ID
    /// </summary>
    Task<UserDetailDto?> GetUserDetailsAsync(string userId);

    /// <summary>
    /// Permanently delete user account and all associated data
    /// </summary>
    Task<bool> DeleteUserAccountAsync(string userId);
}
