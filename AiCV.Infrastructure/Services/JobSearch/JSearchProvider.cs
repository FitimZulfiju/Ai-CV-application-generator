namespace AiCV.Infrastructure.Services.JobSearch;

public class JSearchProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<JSearchProvider> logger) : IJobSearchProvider
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<JSearchProvider> _logger = logger;

    public string ProviderName => "JSearch";

    public async Task<List<JobSearchResult>> SearchAsync(JobSearchQuery query, CancellationToken cancellationToken = default)
    {
        var results = new List<JobSearchResult>();

        var apiKey = query.CustomProperties?.GetValueOrDefault("JSearchApiKey")
            ?? _configuration["JobSearch:JSearch:ApiKey"]
            ?? Environment.GetEnvironmentVariable("AUTOMATION_JSEARCH_API_KEY");

        var apiHost = query.CustomProperties?.GetValueOrDefault("JSearchApiHost")
            ?? _configuration["JobSearch:JSearch:ApiHost"]
            ?? "jsearch.p.rapidapi.com";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("JSearch API key not configured. Skipping JSearch search.");
            return results;
        }

        try
        {
            var keywords = Uri.EscapeDataString(query.Query ?? "");
            var location = Uri.EscapeDataString(query.Location ?? query.Region ?? "");

            // If location is provided, append it to the query as JSearch expects everything in the single "query" parameter
            var searchQuery = string.IsNullOrWhiteSpace(location) ? keywords : $"{keywords} in {location}";

            var url = $"https://{apiHost}/search?query={searchQuery}&page=1&num_pages=1&date_posted=week";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey);
            client.DefaultRequestHeaders.Add("x-rapidapi-host", apiHost);

            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(jsonStream, cancellationToken: cancellationToken);

            // JSearch returns data in a "data" array
            if (document.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataElement.EnumerateArray())
                {
                    if (results.Count >= query.MaxResults)
                        break;

                    var id = item.TryGetProperty("job_id", out var idProp) ? idProp.GetString() : null;
                    if (string.IsNullOrEmpty(id)) continue;

                    var title = item.TryGetProperty("job_title", out var titleProp) ? titleProp.GetString() ?? "" : "";
                    var company = item.TryGetProperty("employer_name", out var companyProp) ? companyProp.GetString() ?? "" : "";

                    var city = item.TryGetProperty("job_city", out var cityProp) ? cityProp.GetString() : "";
                    var country = item.TryGetProperty("job_country", out var countryProp) ? countryProp.GetString() : "";
                    var itemLocation = (string.IsNullOrWhiteSpace(city) ? country : $"{city}, {country}") ?? string.Empty;

                    var snippet = item.TryGetProperty("job_description", out var descProp) ? descProp.GetString() ?? "" : "";

                    var applyUrl = item.TryGetProperty("job_apply_link", out var auProp) ? auProp.GetString() ?? "" : "";

                    DateTime? datePosted = null;
                    if (item.TryGetProperty("job_posted_at_datetime_utc", out var pdProp) && pdProp.TryGetDateTime(out var pd))
                    {
                        datePosted = pd;
                    }

                    results.Add(new JobSearchResult(
                        Id: id,
                        Provider: ProviderName,
                        Title: title,
                        Company: company,
                        Location: itemLocation,
                        DatePosted: datePosted,
                        Url: applyUrl,
                        ApplyUrl: applyUrl,
                        DescriptionSnippet: snippet
                    ));
                }
            }
            else
            {
                _logger.LogWarning("Unexpected JSON structure from JSearch RapidAPI");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching JSearch jobs via RapidAPI");
            throw;
        }

        return results;
    }

    public Task<JobDetail?> GetDetailAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<JobDetail?>(new JobDetail
        {
            Id = jobId,
            Title = "JSearch Job",
            Company = "",
            Location = "",
            ApplyUrl = "",
            Url = "",
            FullDescription = "Detailed view not fully implemented via RapidAPI."
        });
    }
}
