namespace AiCV.Tests.Services;

public class JobPostScraperTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly JobPostScraper _scraper;

    public JobPostScraperTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _scraper = new JobPostScraper(_httpClientFactoryMock.Object);
    }

    private void SetupHttpClientMock(string url, string responseHtml, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var mockMessageHandler = new Mock<HttpMessageHandler>();
        mockMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString() == url),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseHtml)
            });

        var client = new HttpClient(mockMessageHandler.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
    }

    [Fact]
    public async Task ScrapeJobPostingAsync_ValidHtml_ExtractsTitleAndMarkdown()
    {
        // Arrange
        var url = "https://example.com/job/123";
        var html = @"
            <html>
                <head>
                    <title>Software Engineer at TechCorp</title>
                    <meta property=""og:site_name"" content=""TechCorp"">
                </head>
                <body>
                    <article>
                        <h1>Software Engineer</h1>
                        <p>We are looking for a <strong>skilled</strong> developer.</p>
                        <ul><li>C#</li><li>Blazor</li></ul>
                    </article>
                </body>
            </html>";
            
        SetupHttpClientMock(url, html);

        // Act
        var result = await _scraper.ScrapeJobPostingAsync(url);

        // Assert
        Assert.Equal(url, result.Url);
        Assert.Equal("Software Engineer at TechCorp", result.Title); // From HTML Title tag
        Assert.Equal("TechCorp", result.CompanyName); // From OG site_name
        
        // Assert markdown conversion logic
        Assert.Contains("We are looking for a **skilled** developer", result.Description);
        Assert.Contains("- C#", result.Description);
        Assert.Contains("- Blazor", result.Description);
    }

    [Fact]
    public async Task ScrapeJobPostingAsync_EmptyUrl_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _scraper.ScrapeJobPostingAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => _scraper.ScrapeJobPostingAsync("   "));
    }

    [Fact]
    public async Task ScrapeJobPostingAsync_HttpError_ThrowsException()
    {
        // Arrange
        var url = "https://example.com/notfound";
        SetupHttpClientMock(url, "", HttpStatusCode.NotFound);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _scraper.ScrapeJobPostingAsync(url));
        Assert.Contains("Failed to scrape job posting", ex.Message);
    }
}
