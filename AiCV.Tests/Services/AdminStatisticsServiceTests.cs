using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using AiCV.Domain;
using AiCV.Infrastructure.Data;
using AiCV.Infrastructure.Services;

namespace AiCV.Tests.Services;

public class AdminStatisticsServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly AdminStatisticsService _service;

    public AdminStatisticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        var store = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!
        );

        _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<User>())).ReturnsAsync([]);
        _mockUserManager.Setup(x => x.IsLockedOutAsync(It.IsAny<User>())).ReturnsAsync(false);

        _service = new AdminStatisticsService(_context, _mockUserManager.Object);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnCorrectCounts()
    {
        // Arrange
        var user = new User { UserName = "user1", Email = "user1@test.com" };
        var job = new JobPosting
        {
            Title = "Dev",
            CompanyName = "Company A",
            Url = "http://test.com",
        };

        _context.Users.Add(user);
        _context.JobPostings.Add(job);

        _context.GeneratedApplications.AddRange(
            new GeneratedApplication
            {
                User = user,
                JobPosting = job,
                CreatedDate = DateTime.UtcNow,
            },
            new GeneratedApplication
            {
                User = user,
                JobPosting = job,
                CreatedDate = DateTime.UtcNow.AddDays(-1),
            }
        );

        await _context.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatisticsAsync();

        // Assert
        Assert.Equal(2, stats.TotalApplicationsGenerated);
        Assert.Equal(1, stats.TotalUsers);
    }

    [Fact]
    public async Task GetStatisticsCsvAsync_ShouldReturnCsvBytes()
    {
        // Arrange
        var user = new User { UserName = "user1", Email = "user1@test.com" };
        var job = new JobPosting
        {
            Title = "Dev",
            CompanyName = "Company A",
            Url = "http://test.com",
        };

        _context.Users.Add(user);
        _context.JobPostings.Add(job);
        _context.GeneratedApplications.Add(
            new GeneratedApplication
            {
                User = user,
                JobPosting = job,
                CreatedDate = DateTime.UtcNow,
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var bytes = await _service.GetStatisticsCsvAsync();
        var csvContent = Encoding.UTF8.GetString(bytes);

        // Assert
        Assert.NotEmpty(bytes);
        Assert.Contains("Date,User Email,Job Title,Company,Original Job URL", csvContent);
        Assert.Contains("user1@test.com", csvContent);
        Assert.Contains("Company A", csvContent);
    }
}
