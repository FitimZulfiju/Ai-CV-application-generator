namespace AiCV.Infrastructure.Services.JobSearch;

public class LinkedInSearchProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<LinkedInSearchProvider> logger) : IJobSearchProvider
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<LinkedInSearchProvider> _logger = logger;

    public string ProviderName => "LinkedIn";

    public async Task<List<JobSearchResult>> SearchAsync(JobSearchQuery query, CancellationToken cancellationToken = default)
    {
        var results = new List<JobSearchResult>();

        var apiKey = query.CustomProperties?.GetValueOrDefault("LinkedInApiKey")
            ?? _configuration["JobSearch:LinkedIn:ApiKey"]
            ?? Environment.GetEnvironmentVariable("AUTOMATION_LINKEDIN_API_KEY");

        var apiHost = query.CustomProperties?.GetValueOrDefault("LinkedInApiHost")
            ?? _configuration["JobSearch:LinkedIn:ApiHost"]
            ?? "linkedin-jobs-search.p.rapidapi.com";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("LinkedIn API key not configured. Skipping LinkedIn search.");
            return results;
        }

        try
        {
            var keywords = Uri.EscapeDataString(query.Query ?? "");
            var location = Uri.EscapeDataString(query.Location ?? query.Region ?? "Denmark");
            var url = $"https://{apiHost}/search?keywords={keywords}&location={location}&datePosted=anyTime&sort=mostRecent";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-rapidapi-key", apiKey);
            client.DefaultRequestHeaders.Add("x-rapidapi-host", apiHost);

            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(jsonStream, cancellationToken: cancellationToken);

            // Structure depends on RapidAPI exact response, but assume an array or array inside "data"
            JsonElement elements;
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                elements = document.RootElement;
            }
            else if (document.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
            {
                elements = dataElement;
            }
            else
            {
                _logger.LogWarning("Unexpected JSON structure from LinkedIn RapidAPI");
                return results;
            }

            foreach (var item in elements.EnumerateArray())
            {
                if (results.Count >= query.MaxResults)
                    break;

                var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                // Sometimes IDs come back as externalId or something similar
                if (string.IsNullOrEmpty(id) && item.TryGetProperty("externalId", out var extIdProp))
                    id = extIdProp.GetString();

                if (string.IsNullOrEmpty(id)) continue;

                var title = item.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";
                var company = item.TryGetProperty("company", out var companyProp) ? companyProp.GetString() ?? "" : "";
                var itemLocation = item.TryGetProperty("location", out var locProp) ? locProp.GetString() ?? "" : "";
                var snippet = item.TryGetProperty("snippet", out var snipProp) ? snipProp.GetString() ?? "" : "";

                if (string.IsNullOrEmpty(snippet) && item.TryGetProperty("description", out var descProp))
                    snippet = descProp.GetString() ?? "";

                var applyUrl = $"https://www.linkedin.com/jobs/view/{id}";
                if (item.TryGetProperty("applyUrl", out var auProp))
                {
                    applyUrl = auProp.GetString() ?? applyUrl;
                }

                DateTime? datePosted = null;
                if (item.TryGetProperty("postedDate", out var pdProp) && pdProp.TryGetDateTime(out var pd))
                {
                    datePosted = pd;
                }
                else if (item.TryGetProperty("date", out var dProp) && dProp.TryGetDateTime(out var d))
                {
                    datePosted = d;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching LinkedIn jobs via RapidAPI");
        }

        return results;
    }

    public Task<JobDetail?> GetDetailAsync(string jobId, CancellationToken cancellationToken = default)
    {
        // For LinkedIn via this RapidAPI setup, GetDetailAsync isn't requested in the spec,
        // but it's part of the IJobSearchProvider interface. We'll return a placeholder or
        // rely on the scraping method here.
        // Assuming the orchestration gets most details from SearchAsync (like Jobindex does).
        // A full implementation might hit a different RapidAPI endpoint.

        var applyUrl = $"https://www.linkedin.com/jobs/view/{jobId}/";
        return Task.FromResult<JobDetail?>(new JobDetail
        {
            Id = jobId,
            Title = "LinkedIn Job",
            Company = "LinkedIn",
            Location = "",
            ApplyUrl = applyUrl,
            Url = applyUrl,
            FullDescription = "Detailed view not fully implemented via RapidAPI."
        });
    }
}
