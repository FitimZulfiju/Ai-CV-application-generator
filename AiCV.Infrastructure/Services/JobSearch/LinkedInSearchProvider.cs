namespace AiCV.Infrastructure.Services.JobSearch;

public partial class LinkedInSearchProvider(
    IHttpClientFactory httpClientFactory,
    ILogger<LinkedInSearchProvider> logger) : IJobSearchProvider
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<LinkedInSearchProvider> _logger = logger;

    public string ProviderName => "LinkedIn";

    private static readonly System.Threading.Lock RateLimitGate = new();
    private static DateTime _lastRequestAtUtc = DateTime.MinValue;
    private const int MinRequestIntervalMs = 2000;

    private static async Task ThrottleAsync(CancellationToken cancellationToken)
    {
        TimeSpan delay;
        lock (RateLimitGate)
        {
            var now = DateTime.UtcNow;
            var nextAllowed = _lastRequestAtUtc.AddMilliseconds(MinRequestIntervalMs);
            if (now < nextAllowed)
            {
                delay = nextAllowed - now;
                _lastRequestAtUtc = nextAllowed;
            }
            else
            {
                delay = TimeSpan.Zero;
                _lastRequestAtUtc = now;
            }
        }

        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken);
        }
    }

    public async Task<List<JobSearchResult>> SearchAsync(JobSearchQuery query, CancellationToken cancellationToken = default)
    {
        var results = new List<JobSearchResult>();

        try
        {
            var keywords = Uri.EscapeDataString(query.Query ?? "");
            var location = Uri.EscapeDataString(query.Location ?? query.Region ?? "Denmark");
            var url = $"https://www.linkedin.com/jobs-guest/jobs/api/seeMoreJobPostings/search?keywords={keywords}&location={location}&start=0";

            await ThrottleAsync(cancellationToken);

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9,da;q=0.8");

            using var response = await client.GetAsync(url, cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("LinkedIn rate limit reached (429 Too Many Requests) for query '{Query}'. Waiting 3s backoff.", query.Query);
                await Task.Delay(3000, cancellationToken);
                return results;
            }

            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            var nodes = document.DocumentNode.SelectNodes("//li[.//div[contains(@class, 'job-search-card')]]");
            if (nodes == null)
            {
                _logger.LogWarning("No job cards found in LinkedIn HTML response.");
                return results;
            }

            foreach (var node in nodes)
            {
                if (results.Count >= query.MaxResults)
                    break;

                var cardNode = node.SelectSingleNode(".//div[contains(@class, 'job-search-card')]");
                if (cardNode == null) continue;

                var urn = cardNode.GetAttributeValue("data-entity-urn", "");
                var id = urn.Split(':').LastOrDefault();
                if (string.IsNullOrEmpty(id)) continue;

                var titleNode = cardNode.SelectSingleNode(".//h3[contains(@class, 'base-search-card__title')] | .//span[contains(@class, 'sr-only')]");
                var title = titleNode?.InnerText?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(title))
                {
                    titleNode = cardNode.SelectSingleNode(".//span[contains(@class, 'sr-only')]");
                    title = titleNode?.InnerText?.Trim() ?? "";
                }

                title = _htmlTagsRegex.Replace(title, "").Trim();

                var companyNode = cardNode.SelectSingleNode(".//h4[contains(@class, 'base-search-card__subtitle')]");
                var company = companyNode?.InnerText?.Trim() ?? "";
                company = _htmlTagsRegex.Replace(company, "").Trim();

                var locNode = cardNode.SelectSingleNode(".//span[contains(@class, 'job-search-card__location')]");
                var itemLocation = locNode?.InnerText?.Trim() ?? "";

                var urlNode = cardNode.SelectSingleNode(".//a[contains(@class, 'base-card__full-link')]");
                var jobUrl = urlNode?.GetAttributeValue("href", "");

                if (!string.IsNullOrEmpty(jobUrl) && jobUrl.Contains('?'))
                {
                    jobUrl = jobUrl[..jobUrl.IndexOf('?')];
                }
                else if (string.IsNullOrEmpty(jobUrl))
                {
                    jobUrl = $"https://www.linkedin.com/jobs/view/{id}";
                }

                var dateNode = cardNode.SelectSingleNode(".//time[contains(@class, 'job-search-card__listdate')]");
                DateTime? datePosted = null;
                if (dateNode != null)
                {
                    var datetimeStr = dateNode.GetAttributeValue("datetime", "");
                    if (DateTime.TryParse(datetimeStr, out var d)) datePosted = d;
                }

                results.Add(new JobSearchResult(
                    Id: id,
                    Provider: ProviderName,
                    Title: title,
                    Company: company,
                    Location: itemLocation,
                    DatePosted: datePosted,
                    Url: jobUrl,
                    ApplyUrl: jobUrl,
                    DescriptionSnippet: ""
                ));
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("LinkedIn rate limit reached (429 Too Many Requests) for query '{Query}'.", query.Query);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching LinkedIn jobs via public guest API for query '{Query}'", query.Query);
            throw;
        }

        return results;
    }

    public Task<JobDetail?> GetDetailAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var applyUrl = $"https://www.linkedin.com/jobs/view/{jobId}/";
        return Task.FromResult<JobDetail?>(new JobDetail
        {
            Id = jobId,
            Title = "LinkedIn Job",
            Company = "LinkedIn",
            Location = "",
            ApplyUrl = applyUrl,
            Url = applyUrl,
            FullDescription = "Detailed view fetched via scraper."
        });
    }

#pragma warning disable SYSLIB1045 // Use 'GeneratedRegexAttribute'
    private static readonly Regex _htmlTagsRegex = new("<[^>]+>|&nbsp;", RegexOptions.Compiled);
#pragma warning restore SYSLIB1045
}
