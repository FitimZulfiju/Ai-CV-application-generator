namespace AiCV.Tests.Services;

public class UserAIConfigurationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IDbContextFactory<ApplicationDbContext>> _contextFactoryMock;
    private readonly UserAIConfigurationService _service;

    public UserAIConfigurationServiceTests()
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
        var loggerMock = new Mock<ILogger<UserAIConfigurationService>>();

        _service = new UserAIConfigurationService(
            _contextFactoryMock.Object,
            dataProtectionProvider,
            keyManager,
            loggerMock.Object
        );
    }

    [Fact]
    public async Task SaveConfigurationAsync_And_GetConfigurationsAsync_WorksCleanly()
    {
        const string userId = "test-user-1";
        var config = new UserAIConfiguration
        {
            UserId = userId,
            Provider = AIProvider.OpenAI,
            Name = "My OpenAI",
            ApiKey = "sk-test-secret-key-12345",
            ModelId = "gpt-4o",
            IsActive = true
        };

        var saved = await _service.SaveConfigurationAsync(config);
        Assert.NotNull(saved);
        Assert.True(saved.Id > 0);

        var configs = await _service.GetConfigurationsAsync(userId);
        var item = Assert.Single(configs);
        Assert.Equal("My OpenAI", item.Name);
        Assert.Equal("sk-test-secret-key-12345", item.ApiKey);
        Assert.True(item.IsActive);
    }

    [Fact]
    public async Task DeleteConfigurationAsync_RemovesEntity()
    {
        const string userId = "test-user-2";
        var config = new UserAIConfiguration
        {
            UserId = userId,
            Provider = AIProvider.Claude,
            Name = "Claude",
            ApiKey = "sk-ant-test",
            ModelId = "claude-3-5-sonnet",
            IsActive = true
        };

        var saved = await _service.SaveConfigurationAsync(config);
        var deleted = await _service.DeleteConfigurationAsync(saved.Id, userId);

        Assert.True(deleted);
        var configs = await _service.GetConfigurationsAsync(userId);
        Assert.Empty(configs);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
