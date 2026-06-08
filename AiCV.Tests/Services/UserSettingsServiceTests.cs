namespace AiCV.Tests.Services;

public class UserSettingsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IDbContextFactory<ApplicationDbContext>> _contextFactoryMock;
    private readonly Mock<IDataProtectionProvider> _dataProtectionProviderMock;
    private readonly Mock<IDataProtector> _dataProtectorMock;
    private readonly UserSettingsService _service;

    public UserSettingsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        _contextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        _contextFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(() => new ApplicationDbContext(options));

        _dataProtectorMock = new Mock<IDataProtector>();
        _dataProtectorMock.Setup(p => p.Protect(It.IsAny<byte[]>()))
                          .Returns((byte[] data) => data); // Dummy encryption: return same data
        _dataProtectorMock.Setup(p => p.Unprotect(It.IsAny<byte[]>()))
                          .Returns((byte[] data) => data); // Dummy decryption: return same data

        _dataProtectionProviderMock = new Mock<IDataProtectionProvider>();
        _dataProtectionProviderMock.Setup(p => p.CreateProtector(It.IsAny<string>()))
                                   .Returns(_dataProtectorMock.Object);

        _service = new UserSettingsService(_contextFactoryMock.Object, _dataProtectionProviderMock.Object);
    }

    [Fact]
    public async Task GetUserSettingsAsync_NotFound_ReturnsNull()
    {
        var result = await _service.GetUserSettingsAsync("nonexistent_user");
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveUserSettingsAsync_UserNotFound_ThrowsException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.SaveUserSettingsAsync("nonexistent", "key", null, null, null, null, null, AIProvider.OpenAI, "model")
        );
    }

    [Fact]
    public async Task SaveAndGetUserSettingsAsync_ValidUser_EncryptsAndDecrypts()
    {
        // Arrange
        var userId = "user1";
        _dbContext.Users.Add(new User { Id = userId, UserName = "testuser" });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var apiKey = "my_secret_key";
        var provider = AIProvider.OpenAI;
        var model = "gpt-4";

        // Act
        await _service.SaveUserSettingsAsync(userId, apiKey, null, null, null, null, null, provider, model);
        var settings = await _service.GetUserSettingsAsync(userId);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal(userId, settings.UserId);
        Assert.Equal(apiKey, settings.OpenAIApiKey); // Our mock protector just returns the string as-is
        Assert.Equal(provider, settings.DefaultProvider);
        Assert.Equal(model, settings.DefaultModelId);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
