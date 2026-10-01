namespace AiCV.Tests.Services;

public class JobindexSearchProviderTests
{
    private static readonly AutomationOptions TestOptions = new()
    {
        DefaultCron = "0 6 * * *",
        DefaultMaxApplicationsPerRun = 10,
        PollIntervalSeconds = 60,
        Jobindex = new JobindexOptions
        {
            BaseUrl = "https://www.jobindex.dk/jobsoegning.json",
            SearchPageUrl = "https://www.jobindex.dk/jobsoegning",
            RateLimitPerSecond = 1,
        },
    };

    private static JobindexSearchProvider CreateProvider(string responseBody)
    {
        var httpMessageHandler = new MockHttpMessageHandler(responseBody);
        var httpClient = new HttpClient(httpMessageHandler)
        {
            BaseAddress = new Uri("https://www.jobindex.dk/"),
        };
        var httpClientFactory = Mock.Of<IHttpClientFactory>(f => f.CreateClient("Jobindex") == httpClient);

        return new JobindexSearchProvider(
            httpClientFactory,
            Options.Create(TestOptions),
            Mock.Of<ILogger<JobindexSearchProvider>>());
    }

    [Fact]
    public async Task SearchAsync_ValidJson_ReturnsJobs()
    {
        const string json = """
        {
          "result_list_box_html": "<div data-pajob_id=\"h1234567\" data-jtitle=\".NET Developer\" data-pajob_company=\"Acme\" data-location=\"Copenhagen\"><div class=\"description\">C# and ASP.NET Core</div><time datetime=\"2026-09-10\"></time></div>"
        }
        """;
        var provider = CreateProvider(json);

        var results = await provider.SearchAsync(
            new JobSearchQuery(".NET Copenhagen", "Jobindex", "Copenhagen", null, 20, 7),
            CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("h1234567", results[0].Id);
        Assert.Equal(".NET Developer", results[0].Title);
        Assert.Equal("Acme", results[0].Company);
        // No href in the markup, so the id-based fallback URL is used.
        Assert.Equal("https://www.jobindex.dk/vis-job/h1234567", results[0].Url);
        Assert.Equal(new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc), results[0].DatePosted);
    }

    /// <summary>
    /// The scraped href is the real advert link and must win over a synthesized URL — it is
    /// fetched later and scraped to feed AI generation.
    /// </summary>
    [Fact]
    public async Task SearchAsync_ShouldPreferScrapedHrefOverSynthesizedUrl()
    {
        const string json = """
        {
          "result_list_box_html": "<div data-pajob_id=\"h1234567\" data-jtitle=\".NET Developer\"><a href=\"/vis-job/h1234567/some-company-title\">.NET Developer</a></div>"
        }
        """;
        var provider = CreateProvider(json);

        var results = await provider.SearchAsync(
            new JobSearchQuery(".NET", "Jobindex", null, null, 20, 7),
            CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("https://www.jobindex.dk/vis-job/h1234567/some-company-title", results[0].Url);
    }

    [Fact]
    public async Task SearchAsync_ShouldPassRegionToProvider()
    {
        var handler = new MockHttpMessageHandler("{}");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://www.jobindex.dk/") };
        var factory = Mock.Of<IHttpClientFactory>(f => f.CreateClient("Jobindex") == httpClient);
        var provider = new JobindexSearchProvider(
            factory,
            Options.Create(TestOptions),
            Mock.Of<ILogger<JobindexSearchProvider>>());

        await provider.SearchAsync(
            new JobSearchQuery(".NET", "Jobindex", "København", "storkoebenhavn", 20, 7),
            CancellationToken.None);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("region=storkoebenhavn", handler.LastRequestUri!.Query, StringComparison.Ordinal);
        Assert.Contains("location=K%C3%B8benhavn", handler.LastRequestUri.Query, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("2026-09-10", 2026, 9, 10)]
    [InlineData("10. september 2026", 2026, 9, 10)]
    [InlineData("10. sep. 2026", 2026, 9, 10)]
    [InlineData("10-09-2026", 2026, 9, 10)]
    public void ParseDate_ShouldParseDanishFormats(string input, int year, int month, int day)
    {
        var parsed = JobindexSearchProvider.ParseDate(input);

        Assert.NotNull(parsed);
        Assert.Equal(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc), parsed!.Value.Date);
    }

    [Theory]
    [InlineData("i dag", 0)]
    [InlineData("i går", -1)]
    [InlineData("for 3 dage siden", -3)]
    public void ParseDate_ShouldParseRelativeDanishPhrases(string input, int dayOffset)
    {
        var parsed = JobindexSearchProvider.ParseDate(input);

        Assert.NotNull(parsed);
        Assert.Equal(DateTime.UtcNow.Date.AddDays(dayOffset), parsed!.Value.Date);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a date at all")]
    public void ParseDate_ShouldReturnNullForUnparseableValues(string? input)
    {
        Assert.Null(JobindexSearchProvider.ParseDate(input));
    }

    [Fact]
    public async Task SearchAsync_FallsBackToHtmlWhenNotJson()
    {
        const string html = """
        <div class="jobsearch-result">
            <a href="/jobsoegning/h7654321" class="title">ASP.NET Core Engineer</a>
            <div class="company">Beta</div>
            <div class="location">Aarhus</div>
            <div class="description">Blazor and Azure</div>
        </div>
        """;
        var provider = CreateProvider(html);

        var results = await provider.SearchAsync(
            new JobSearchQuery("ASP.NET Core", "Jobindex", null, null, 20, 7),
            CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("h7654321", results[0].Id);
        Assert.Contains("ASP.NET Core", results[0].Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchAsync_BotProtection_ReturnsEmpty()
    {
        var provider = CreateProvider("<html><body>captcha</body></html>");
        var results = await provider.SearchAsync(
            new JobSearchQuery("query", "Jobindex", null, null, 20, 7),
            CancellationToken.None);
        Assert.Empty(results);
    }

    private sealed class MockHttpMessageHandler(string responseBody) : HttpMessageHandler
    {
        private readonly string _responseBody = responseBody;

        /// <summary>URI of the most recent request, for asserting on query-string construction.</summary>
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "text/html"),
            };
            return Task.FromResult(response);
        }
    }
}