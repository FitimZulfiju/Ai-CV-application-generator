namespace AiCV.Tests.Services;

public class LinkedInSearchProviderTests
{
    [Fact]
    public async Task SearchAsync_Should_ParseJobsFromJson()
    {
        const string html = """
        <li class="result-card">
            <div class="base-card relative w-full hover:no-underline focus:no-underline base-card--link base-search-card base-search-card--link job-search-card" data-entity-urn="urn:li:jobPosting:12345">
                <a class="base-card__full-link" href="https://linkedin.com/apply/12345"></a>
                <div class="base-search-card__info">
                    <h3 class="base-search-card__title">Software Engineer</h3>
                    <h4 class="base-search-card__subtitle">Tech Corp</h4>
                    <div class="base-search-card__metadata">
                        <span class="job-search-card__location">Copenhagen</span>
                        <time class="job-search-card__listdate" datetime="2023-10-01"></time>
                    </div>
                </div>
            </div>
        </li>
""";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(html)
            });

        var client = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["JobSearch:LinkedIn:ApiKey"]).Returns("fake-api-key");
        configMock.Setup(c => c["JobSearch:LinkedIn:ApiHost"]).Returns("fake-host.com");

        var loggerMock = new Mock<ILogger<LinkedInSearchProvider>>();

        var provider = new LinkedInSearchProvider(factoryMock.Object, loggerMock.Object);

        var query = new JobSearchQuery("test");
        var results = await provider.SearchAsync(query, CancellationToken.None);

        var item = Assert.Single(results);
        var job = item;
        Assert.Equal("12345", job.Id);
        Assert.Equal("Software Engineer", job.Title);
        Assert.Equal("Tech Corp", job.Company);
        Assert.Equal("Copenhagen", job.Location);
        Assert.Equal("https://linkedin.com/apply/12345", job.ApplyUrl);
        Assert.Equal(new DateTime(2023, 10, 1, 0, 0, 0, DateTimeKind.Utc).ToLocalTime(), job.DatePosted?.ToLocalTime());
    }
}

