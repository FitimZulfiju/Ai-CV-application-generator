namespace AiCV.Tests.Services;

public class UserSettingsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IDbContextFactory<ApplicationDbContext>> _contextFactoryMock;
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

        var services = new ServiceCollection()
            .AddDataProtection()
            .Services.BuildServiceProvider();
        var dataProtectionProvider = services.GetRequiredService<IDataProtectionProvider>();
        var keyManager = services.GetRequiredService<IKeyManager>();
        var loggerMock = new Mock<ILogger<UserSettingsService>>();

        _service = new UserSettingsService(
            _contextFactoryMock.Object,
            dataProtectionProvider,
            keyManager,
            loggerMock.Object
        );
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
        const string userId = "user1";
        _dbContext.Users.Add(new User { Id = userId, UserName = "testuser" });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        const string apiKey = "my_secret_key";
        const AIProvider provider = AIProvider.OpenAI;
        const string model = "gpt-4";

        // Act
        await _service.SaveUserSettingsAsync(userId, apiKey, null, null, null, null, null, provider, model);
        var settings = await _service.GetUserSettingsAsync(userId);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal(userId, settings.UserId);
        Assert.Equal(apiKey, settings.OpenAIApiKey);
        Assert.Equal(provider, settings.DefaultProvider);
        Assert.Equal(model, settings.DefaultModelId);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
