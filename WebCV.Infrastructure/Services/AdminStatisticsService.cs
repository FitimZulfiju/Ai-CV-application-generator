namespace WebCV.Infrastructure.Services;

/// <summary>
/// Service for retrieving admin statistics
/// </summary>
public class AdminStatisticsService(ApplicationDbContext context, UserManager<User> userManager)
    : IAdminStatisticsService
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<User> _userManager = userManager;

    public async Task<AdminStatisticsDto> GetStatisticsAsync()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
        var startOfDay = now.Date;

        var totalUsers = await _context.Users.CountAsync();
        var totalProfiles = await _context.CandidateProfiles.CountAsync();
        var totalApplications = await _context.GeneratedApplications.CountAsync();

        var applicationsThisMonth = await _context
            .GeneratedApplications.Where(a => a.CreatedDate >= startOfMonth)
            .CountAsync();

        var applicationsThisWeek = await _context
            .GeneratedApplications.Where(a => a.CreatedDate >= startOfWeek)
            .CountAsync();

        var applicationsToday = await _context
            .GeneratedApplications.Where(a => a.CreatedDate >= startOfDay)
            .CountAsync();

        var recentApplications = await _context
            .GeneratedApplications.OrderByDescending(a => a.CreatedDate)
            .Take(10)
            .Select(a => new RecentApplicationDto
            {
                Id = a.Id,
                UserEmail = a.User != null ? a.User.Email ?? "Unknown" : "Unknown",
                JobTitle = a.JobPosting != null ? a.JobPosting.Title : "Unknown",
                CompanyName = a.JobPosting != null ? a.JobPosting.CompanyName : "Unknown",
                CreatedDate = a.CreatedDate,
            })
            .ToListAsync();

        // Phase 2: Daily application counts for last 30 days
        var thirtyDaysAgo = now.AddDays(-30).Date;
        var dailyCounts = await _context
            .GeneratedApplications.Where(a => a.CreatedDate >= thirtyDaysAgo)
            .GroupBy(a => a.CreatedDate.Date)
            .Select(g => new DailyCountDto { Date = g.Key, Count = g.Count() })
            .OrderBy(d => d.Date)
            .ToListAsync();

        // Fill in missing days with zero counts
        var allDays = new List<DailyCountDto>();
        for (var date = thirtyDaysAgo; date <= now.Date; date = date.AddDays(1))
        {
            var existing = dailyCounts.FirstOrDefault(d => d.Date.Date == date.Date);
            allDays.Add(new DailyCountDto { Date = date, Count = existing?.Count ?? 0 });
        }

        // Phase 2: Top users by application count
        var topUsers = await _context
            .GeneratedApplications.GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        var topUserDtos = new List<TopUserDto>();
        foreach (var top in topUsers)
        {
            var user = await _context
                .Users.Include(u => u.CandidateProfile)
                .FirstOrDefaultAsync(u => u.Id == top.UserId);
            if (user != null)
            {
                topUserDtos.Add(
                    new TopUserDto
                    {
                        Email = user.Email ?? "Unknown",
                        FullName = user.CandidateProfile?.FullName ?? user.Email ?? "Unknown",
                        ApplicationCount = top.Count,
                    }
                );
            }
        }

        // Get detailed user information
        var users = await _context
            .Users.Include(u => u.CandidateProfile)
            .Include(u => u.UserSettings)
            .Include(u => u.GeneratedApplications)
            .ToListAsync();

        var userDetails = new List<UserDetailDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLockedOut = await _userManager.IsLockedOutAsync(user);
            userDetails.Add(
                new UserDetailDto
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
                }
            );
        }

        return new AdminStatisticsDto
        {
            TotalUsers = totalUsers,
            TotalProfiles = totalProfiles,
            TotalApplicationsGenerated = totalApplications,
            ApplicationsThisMonth = applicationsThisMonth,
            ApplicationsThisWeek = applicationsThisWeek,
            ApplicationsToday = applicationsToday,
            RecentApplications = recentApplications,
            Users = userDetails,
            DailyApplicationCounts = allDays,
            TopUsersByApplications = topUserDtos,
        };
    }

    public async Task<byte[]> GetStatisticsCsvAsync()
    {
        var applications = await _context
            .GeneratedApplications.Include(a => a.User)
            .Include(a => a.JobPosting)
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Date,User Email,Job Title,Company,Original Job URL");

        foreach (var app in applications)
        {
            var date = app.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
            var email = app.User?.Email ?? "Unknown";
            var title = app.JobPosting?.Title?.Replace(",", " ") ?? "Unknown";
            var company = app.JobPosting?.CompanyName?.Replace(",", " ") ?? "Unknown";
            var url = app.JobPosting?.Url?.Replace(",", " ") ?? "";

            csv.AppendLine($"{date},{email},{title},{company},{url}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }
}
