using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using WebCV.Domain;
using WebCV.Infrastructure.Data;
using WebCV.Infrastructure.Services;
using Xunit;

namespace WebCV.Tests.Services;

public class CVServiceTests
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public CVServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Create a mock factory that returns a new context with the in-memory options
        var mockFactory = new Moq.Mock<IDbContextFactory<ApplicationDbContext>>();
        mockFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new ApplicationDbContext(options));

        _contextFactory = mockFactory.Object;
    }

    [Fact]
    public async Task GetProfileAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var service = new CVService(_contextFactory);
        var userId = "non-existent-user";

        // Act
        var result = await service.GetProfileAsync(userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProfileAsync_ShouldCreateProfile_WhenUserExistsAndProfileMissing()
    {
        // Arrange
        var userId = "new-user-with-no-profile";
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            context.Users.Add(new User { Id = userId, UserName = "testuser", Email = "test@example.com" });
            await context.SaveChangesAsync();
        }

        var service = new CVService(_contextFactory);

        // Act
        var result = await service.GetProfileAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        
        // Verify it was saved to DB
        // Verify it was saved to DB
        using var verifyContext = await _contextFactory.CreateDbContextAsync();
        var savedProfile = await verifyContext.CandidateProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        Assert.NotNull(savedProfile);
    }

    [Fact]
    public async Task GetProfileAsync_ShouldReturnExistingProfile_WhenUserExists()
    {
        // Arrange
        var userId = "existing-user";
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            context.CandidateProfiles.Add(new CandidateProfile
            {
                UserId = userId,
                FullName = "Test User",
                Title = "Developer"
            });
            await context.SaveChangesAsync();
        }

        var service = new CVService(_contextFactory);

        // Act
        var result = await service.GetProfileAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("Test User", result.FullName);
    }

    [Fact]
    public async Task SaveProfileAsync_ShouldUpdateExistingProfile()
    {
        // Arrange
        var userId = "update-user";
        var profile = new CandidateProfile { UserId = userId, FullName = "Original Name" };
        
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            context.CandidateProfiles.Add(profile);
            await context.SaveChangesAsync();
        }

        var service = new CVService(_contextFactory);
        
        // Modify profile
        profile.FullName = "Updated Name";

        // Act
        await service.SaveProfileAsync(profile);

        // Assert
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var updatedProfile = await context.CandidateProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            Assert.NotNull(updatedProfile);
            Assert.Equal("Updated Name", updatedProfile.FullName);
        }
    }
}
