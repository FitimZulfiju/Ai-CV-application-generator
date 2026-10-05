namespace AiCV.Tests.Services;

public class LinkedInSearchProviderTests
{
    [Fact]
    public async Task SearchAsync_Should_ParseJobsFromJson()
    {
        const string json = """

        {
            "data": [
                {
                    "id": "12345",
                    "title": "Software Engineer",
                    "company": "Tech Corp",
                    "location": "Copenhagen",
                    "applyUrl": "https://linkedin.com/apply/12345",
                    "postedDate": "2023-10-01T00:00:00Z"
                }
            ]
        }
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
                Content = new StringContent(json)
            });

        var client = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["JobSearch:LinkedIn:ApiKey"]).Returns("fake-api-key");
        configMock.Setup(c => c["JobSearch:LinkedIn:ApiHost"]).Returns("fake-host.com");

        var loggerMock = new Mock<ILogger<LinkedInSearchProvider>>();

        var provider = new LinkedInSearchProvider(factoryMock.Object, configMock.Object, loggerMock.Object);

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
