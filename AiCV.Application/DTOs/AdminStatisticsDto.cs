namespace AiCV.Application.DTOs;

/// <summary>
/// DTO containing admin dashboard statistics
/// </summary>
public class AdminStatisticsDto
{
    public int TotalUsers { get; set; }
    public int TotalProfiles { get; set; }
    public int TotalApplicationsGenerated { get; set; }
    public int ApplicationsThisMonth { get; set; }
    public int ApplicationsThisWeek { get; set; }
    public int ApplicationsToday { get; set; }
    public List<RecentApplicationDto> RecentApplications { get; set; } = [];
    public List<UserDetailDto> Users { get; set; } = [];

    // Phase 2: Chart data
    public List<DailyCountDto> DailyApplicationCounts { get; set; } = [];
    public List<TopUserDto> TopUsersByApplications { get; set; } = [];
}

/// <summary>
/// DTO for recent application display
/// </summary>
public class RecentApplicationDto
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// DTO for user details in admin dashboard
/// </summary>
public class UserDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool HasProfile { get; set; }
    public bool HasApiKeys { get; set; }
    public int ApplicationsGenerated { get; set; }
    public List<string> Roles { get; set; } = [];
    public DateTime? LastLoginDate { get; set; }
    public DateTime RegisteredDate { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
}

/// <summary>
/// DTO for daily application count (for charts)
/// </summary>
public class DailyCountDto
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// DTO for top users by applications
/// </summary>
public class TopUserDto
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int ApplicationCount { get; set; }
}
