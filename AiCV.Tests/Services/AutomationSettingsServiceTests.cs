namespace AiCV.Tests.Services;

public class AutomationSettingsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IDbContextFactory<ApplicationDbContext>> _contextFactoryMock;
    private readonly AutomationSettingsService _service;

    public AutomationSettingsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        _contextFactoryMock = new Mock<IDbContextFactory<ApplicationDbContext>>();
        _contextFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(() => new ApplicationDbContext(options));

        var automationOptions = Options.Create(new AutomationOptions
        {
            DefaultCron = "0 6 * * *",
            DefaultMaxApplicationsPerRun = 5
        });

        var loggerMock = new Mock<ILogger<AutomationSettingsService>>();

        _service = new AutomationSettingsService(
            _contextFactoryMock.Object,
            automationOptions,
            loggerMock.Object
        );
    }

    [Fact]
    public async Task GetOrCreateAsync_CreatesDefaultQueries()
    {
        const string userId = "user-123";

        var settings = await _service.GetOrCreateAsync(userId);

        Assert.NotNull(settings);
        Assert.Equal(userId, settings.UserId);
        Assert.NotEmpty(settings.Queries);
        Assert.All(settings.Queries, q =>
        {
            Assert.NotEmpty(q.Query);
            Assert.Equal("Jobindex", q.Provider);
            Assert.True(q.IsActive);
        });
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingQueriesAndAddsNewQueries()
    {
        const string userId = "user-456";
        var initialSettings = await _service.GetOrCreateAsync(userId);
        Assert.NotNull(initialSettings);

        var firstQuery = initialSettings.Queries[0];
        int initialQueryId = firstQuery.Id;

        firstQuery.Query = "Senior .NET Developer, Lead Architect";
        firstQuery.Location = "Aarhus";
        firstQuery.Provider = "LinkedIn";
        firstQuery.MaxResults = 50;
        firstQuery.JobAgeDays = 14;
        firstQuery.IsActive = false;

        var newQuery = new AutomationQuery
        {
            Query = "Fullstack Blazor, React",
            Location = "Remote",
            Provider = "Jobindex",
            MaxResults = 30,
            JobAgeDays = 10,
            IsActive = true
        };

        initialSettings.Queries.Add(newQuery);

        var updated = await _service.UpdateAsync(initialSettings, userId);

        Assert.NotNull(updated);
        var retrievedFirst = updated.Queries.FirstOrDefault(q => q.Id == initialQueryId);
        Assert.NotNull(retrievedFirst);
        Assert.Equal("Senior .NET Developer, Lead Architect", retrievedFirst.Query);
        Assert.Equal("Aarhus", retrievedFirst.Location);
        Assert.Equal("LinkedIn", retrievedFirst.Provider);
        Assert.Equal(50, retrievedFirst.MaxResults);
        Assert.Equal(14, retrievedFirst.JobAgeDays);
        Assert.False(retrievedFirst.IsActive);

        var retrievedNew = updated.Queries.FirstOrDefault(q => q.Query == "Fullstack Blazor, React");
        Assert.NotNull(retrievedNew);
        Assert.True(retrievedNew.Id > 0);
        Assert.Equal("Remote", retrievedNew.Location);
    }

    [Fact]
    public async Task UpdateAsync_RemovesQueriesNotInIncomingList()
    {
        const string userId = "user-789";
        var initialSettings = await _service.GetOrCreateAsync(userId);
        Assert.NotNull(initialSettings);
        Assert.True(initialSettings.Queries.Count > 1);

        var keptQuery = initialSettings.Queries[0];
        initialSettings.Queries = [keptQuery];

        var updated = await _service.UpdateAsync(initialSettings, userId);

        var item = Assert.Single(updated.Queries);
        Assert.Equal(keptQuery.Id, item.Id);
    }

    [Fact]
    public async Task UpdateAsync_DuplicatesExistingQueryAsNewEntity()
    {
        const string userId = "user-duplicate-test";
        var initialSettings = await _service.GetOrCreateAsync(userId);
        Assert.NotNull(initialSettings);

        var queryToDuplicate = initialSettings.Queries[0];
        int originalCount = initialSettings.Queries.Count;

        var duplicate = new AutomationQuery
        {
            Query = queryToDuplicate.Query,
            Provider = queryToDuplicate.Provider,
            Location = queryToDuplicate.Location,
            Region = queryToDuplicate.Region,
            MaxResults = queryToDuplicate.MaxResults,
            JobAgeDays = queryToDuplicate.JobAgeDays,
            IsActive = queryToDuplicate.IsActive
        };

        initialSettings.Queries.Insert(1, duplicate);

        var updated = await _service.UpdateAsync(initialSettings, userId);

        Assert.Equal(originalCount + 1, updated.Queries.Count);
        var insertedDuplicate = updated.Queries.FirstOrDefault(q => q.Id != queryToDuplicate.Id && q.Query == queryToDuplicate.Query);
        Assert.NotNull(insertedDuplicate);
        Assert.True(insertedDuplicate.Id > 0);
        Assert.NotEqual(queryToDuplicate.Id, insertedDuplicate.Id);
        Assert.Equal(queryToDuplicate.Location, insertedDuplicate.Location);
        Assert.Equal(queryToDuplicate.Provider, insertedDuplicate.Provider);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}

