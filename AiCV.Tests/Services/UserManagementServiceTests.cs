namespace AiCV.Tests.Services;

public class UserManagementServiceTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly UserManagementService _service;

    public UserManagementServiceTests()
    {
        var userStoreMock = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            userStoreMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!
        );

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB for each test
            .Options;

        // We can use a real InMemory DbContext or mock it. use InMemory for simplicity with DbSet.
        // But since the service takes DbContext, let's try to pass a real one with in-memory provider.
        // However, the service constructor takes ApplicationDbContext.
        var dbContext = new ApplicationDbContext(options);

        _service = new UserManagementService(dbContext, _userManagerMock.Object);
    }

    [Fact]
    public async Task ToggleUserLockoutAsync_UserNotFound_ReturnsFalse()
    {
        // Arrange
        _userManagerMock.Setup(x => x.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        // Act
        var result = await _service.ToggleUserLockoutAsync("nonexistent", true);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ToggleUserLockoutAsync_LockUser_EnablesLockout_SetsEndDate_UpdatesSecurityStamp()
    {
        // Arrange
        var user = new User { Id = "user1", Email = "test@example.com" };
        _userManagerMock.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

        _userManagerMock.Setup(x => x.GetLockoutEnabledAsync(user)).ReturnsAsync(false); // Simulate lockout disabled

        _userManagerMock
            .Setup(x => x.SetLockoutEnabledAsync(user, true))
            .Returns(Task.FromResult(IdentityResult.Success));

        _userManagerMock
            .Setup(x => x.SetLockoutEndDateAsync(user, It.IsAny<DateTimeOffset>()))
            .Returns(Task.FromResult(IdentityResult.Success));

        _userManagerMock
            .Setup(x => x.UpdateSecurityStampAsync(user))
            .Returns(Task.FromResult(IdentityResult.Success));

        // Act
        var result = await _service.ToggleUserLockoutAsync("user1", true);

        // Assert
        Assert.True(result);
        _userManagerMock.Verify(x => x.SetLockoutEnabledAsync(user, true), Times.Once);
        _userManagerMock.Verify(
            x =>
                x.SetLockoutEndDateAsync(
                    user,
                    It.Is<DateTimeOffset>(d => d > DateTimeOffset.UtcNow.AddYears(90))
                ),
            Times.Once
        );
        _userManagerMock.Verify(x => x.UpdateSecurityStampAsync(user), Times.Once);
    }

    [Fact]
    public async Task ToggleUserLockoutAsync_UnlockUser_RemovesLockout()
    {
        // Arrange
        var user = new User { Id = "user1", Email = "test@example.com" };
        _userManagerMock.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.SetLockoutEndDateAsync(user, null))
            .Returns(Task.FromResult(IdentityResult.Success));

        // Act
        var result = await _service.ToggleUserLockoutAsync("user1", false);

        // Assert
        Assert.True(result);
        _userManagerMock.Verify(
            x => x.SetLockoutEnabledAsync(It.IsAny<User>(), It.IsAny<bool>()),
            Times.Never
        ); // Should not change enabled status
        _userManagerMock.Verify(x => x.SetLockoutEndDateAsync(user, null), Times.Once);
        _userManagerMock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Never); // Should not reset security stamp on unlock usually, unless needed. Protocol was: only on lock.
    }
}
