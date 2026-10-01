namespace AiCV.Infrastructure.Services.JobSearch;

public partial class JobindexSearchProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<AutomationOptions> options,
    ILogger<JobindexSearchProvider> logger
) : IJobSearchProvider
{
    private const string ClientName = "Jobindex";
    private const string SiteBaseUrl = "https://www.jobindex.dk";

    [GeneratedRegex(@"\bh(\d{6,8})\b", RegexOptions.IgnoreCase)]
    private static partial Regex JobIdRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"(?<days>\d{1,3})\s*(dage?|days?)\s*(siden|ago)", RegexOptions.IgnoreCase)]
    private static partial Regex RelativeDaysRegex();

    [GeneratedRegex(@"^\s*(mandag|tirsdag|onsdag|torsdag|fredag|lordag|sondag|man|tir|ons|tor|fre|lor|son|monday|tuesday|wednesday|thursday|friday|saturday|sunday)?\s*[,.]?\s*(d\.?\s*)?", RegexOptions.IgnoreCase)]
    private static partial Regex LeadingDatePrefixRegex();

    private static readonly CultureInfo DanishCulture = CultureInfo.GetCultureInfo("da-DK");

    private static readonly string[] DanishDateFormats =
    [
        "d. MMMM yyyy", "d. MMM yyyy", "d. MMM. yyyy", "dd. MMMM yyyy", "dd. MMM yyyy",
        "d. MMMM", "d. MMM", "d. MMM.",
        "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy", "dd/MM", "d/M",
        "yyyy-MM-dd",
    ];

    private static readonly System.Threading.Lock RateLimitGate = new();
    private static DateTime _lastRequestAtUtc = DateTime.MinValue;

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IOptions<AutomationOptions> _options = options;
    private readonly ILogger<JobindexSearchProvider> _logger = logger;

    public string ProviderName => "Jobindex";

    public async Task<List<JobSearchResult>> SearchAsync(JobSearchQuery query, CancellationToken cancellationToken = default)
    {
        var opts = _options.Value.Jobindex;
        var jobAge = Math.Clamp(query.JobAgeDays, 1, 9999);
        var url = BuildUrl(opts.BaseUrl, query.Query, query.Location, query.Region, 1, jobAge);

        await ThrottleAsync(cancellationToken);
        var raw = await GetStringWithClientAsync(url, cancellationToken);

        var results = ParseJsonResponse(raw);

        if (results.Count == 0)
        {
            results = ParseResultsHtml(raw);
        }

        if (results.Count == 0 && !string.IsNullOrWhiteSpace(opts.SearchPageUrl))
        {
            var pageUrl = BuildUrl(opts.SearchPageUrl, query.Query, query.Location, query.Region, 1, jobAge);
            await ThrottleAsync(cancellationToken);
            var pageRaw = await GetStringWithClientAsync(pageUrl, cancellationToken);
            results = ParseResultsHtml(pageRaw);
        }

        return [.. results.Take(Math.Max(1, query.MaxResults))];
    }

    public async Task<JobDetail?> GetDetailAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return null;
        }

        var url = BuildJobUrl(jobId);
        await ThrottleAsync(cancellationToken);
        var html = await GetStringWithClientAsync(url, cancellationToken);
        return ParseDetailPage(html, jobId, url);
    }

    private static string BuildUrl(string baseUrl, string query, string? location, string? region, int page, int jobAge)
    {
        var separator = baseUrl.Contains('?') ? '&' : '?';
        var url = string.Concat(
            baseUrl,
            separator, "q=", Uri.EscapeDataString(query),
            "&page=", page.ToString(CultureInfo.InvariantCulture),
            "&jobage=", jobAge.ToString(CultureInfo.InvariantCulture),
            "&sort=date"
        );
        if (!string.IsNullOrWhiteSpace(location))
        {
            url += "&location=" + Uri.EscapeDataString(location);
        }

        if (!string.IsNullOrWhiteSpace(region))
        {
            url += "&region=" + Uri.EscapeDataString(region);
        }

        return url;
    }

    private async Task<string> GetStringWithClientAsync(string url, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(ClientName);
        return await client.GetStringAsync(url, cancellationToken);
    }

    private List<JobSearchResult> ParseJsonResponse(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            if (root.TryGetProperty("result_list_box_html", out var box))
            {
                var boxHtml = box.GetString();
                if (!string.IsNullOrWhiteSpace(boxHtml))
                {
                    return ParseResultsHtml(boxHtml);
                }
            }

            if (root.TryGetProperty("jobs", out var jobs))
            {
                return ParseJobsArray(jobs);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Jobindex response was not valid JSON; falling back to HTML parsing.");
        }
        return [];
    }

    private static string? GetSafeString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }

    private static List<JobSearchResult> ParseJobsArray(JsonElement jobs)
    {
        var results = new List<JobSearchResult>();
        if (jobs.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var item in jobs.EnumerateArray())
        {
            var id = GetJsonProperty(item, "pajob_id", "id", "job_id");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var title = GetJsonProperty(item, "jtitle", "title", "job_title");
            var company = GetJsonProperty(item, "pajob_company", "company", "company_name");
            var location = GetJsonProperty(item, "location", "job_location");
            var description = GetJsonProperty(item, "description", "job_description", "pajob_description");
            var dateText = GetJsonProperty(item, "date", "date_posted", "time");

            var jobUrl = BuildJobUrl(id);
            results.Add(new JobSearchResult(
                Id: id,
                Provider: "Jobindex",
                Title: Clean(title),
                Company: Clean(company),
                Location: Clean(location),
                DatePosted: ParseDate(dateText),
                Url: jobUrl,
                ApplyUrl: jobUrl,
                DescriptionSnippet: Clean(description, maxLength: 500)
            ));
        }
        return results;
    }

    [GeneratedRegex(@"var\s+Stash\s*=\s*(\{.*?\})\s*;\s*$", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex StashRegex();

    internal static List<JobSearchResult> ParseResultsHtml(string html)
    {
        var results = new List<JobSearchResult>();
        if (string.IsNullOrWhiteSpace(html))
        {
            return results;
        }

        if (html.Contains("captcha", StringComparison.OrdinalIgnoreCase))
        {
            return results;
        }

        var stashMatch = StashRegex().Match(html);
        if (stashMatch.Success)
        {
            try
            {
                var stashJson = stashMatch.Groups[1].Value;
                using var doc = JsonDocument.Parse(stashJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("jobsearch/result_app", out var app) &&
                    app.TryGetProperty("storeData", out var storeData) &&
                    storeData.TryGetProperty("searchResponse", out var searchResponse) &&
                    searchResponse.TryGetProperty("results", out var resultsArray))
                {
                    var stashSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var item in resultsArray.EnumerateArray())
                    {
                        var tid = GetSafeString(item, "tid");
                        if (string.IsNullOrEmpty(tid) || !stashSeen.Add(tid))
                        {
                            continue;
                        }

                        var headline = GetSafeString(item, "headline");

                        var company = "";
                        if (item.TryGetProperty("company", out var companyProp))
                        {
                            if (companyProp.ValueKind == JsonValueKind.String)
                                company = companyProp.GetString();
                            else if (companyProp.ValueKind == JsonValueKind.Object && companyProp.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String)
                                company = nameProp.GetString();
                        }

                        var area = GetSafeString(item, "area");
                        var shareUrl = GetSafeString(item, "share_url");
                        var applyUrl = GetSafeString(item, "url") ?? "";
                        var lastDate = GetSafeString(item, "lastdate");
                        var snippet = GetSafeString(item, "companytext");

                        var jobUrl = !string.IsNullOrWhiteSpace(shareUrl) ? shareUrl : BuildJobUrl(tid);

                        if (string.IsNullOrWhiteSpace(applyUrl)) applyUrl = jobUrl;

                        results.Add(new JobSearchResult(
                            Id: tid,
                            Provider: "Jobindex",
                            Title: Clean(headline),
                            Company: Clean(company),
                            Location: Clean(area),
                            DatePosted: ParseDate(lastDate),
                            Url: jobUrl,
                            ApplyUrl: applyUrl,
                            DescriptionSnippet: Clean(snippet, maxLength: 500)
                        ));
                    }

                    return results;
                }
            }
            catch (JsonException)
            {
                //
            }
        }
        else
        {
            //
        }

        var docNode = new HtmlDocument();
        docNode.LoadHtml(html);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var nodes = docNode.DocumentNode.SelectNodes("//*[@data-pajob_id]")?.ToList() ?? [];
        foreach (var node in nodes)
        {
            var id = FirstAttribute(node, "data-pajob_id", "data-jobid", "data-id");
            if (string.IsNullOrEmpty(id) || !seen.Add(id))
            {
                continue;
            }

            var title = FirstNonEmpty(
                FirstAttribute(node, "data-jtitle", "data-title"),
                node.SelectSingleNode(".//*[contains(@class,'title') or self::h1 or self::h2 or self::h3]")?.InnerText);
            var company = FirstNonEmpty(
                FirstAttribute(node, "data-pajob_company", "data-company"),
                node.SelectSingleNode(".//*[contains(@class,'company')]")?.InnerText);
            var location = FirstNonEmpty(
                FirstAttribute(node, "data-location"),
                node.SelectSingleNode(".//*[contains(@class,'location')]")?.InnerText);
            var snippet = node.SelectSingleNode(".//*[contains(@class,'description') or contains(@class,'snippet')]")?.InnerText
                ?? string.Empty;
            var dateText = node.SelectSingleNode(".//time")?.GetAttributeValue("datetime", "")
                ?? node.SelectSingleNode(".//*[contains(@class,'date')]")?.InnerText
                ?? string.Empty;
            var href = node.SelectSingleNode(".//a[@href]")?.GetAttributeValue("href", "");

            var jobUrl = ResolveJobUrl(href, id);
            results.Add(new JobSearchResult(
                id,
                "Jobindex",
                Clean(title),
                Clean(company),
                Clean(location),
                ParseDate(dateText),
                jobUrl,
                jobUrl,
                Clean(snippet, maxLength: 500)
            ));
        }

        if (results.Count == 0)
        {
            var classNodes = docNode.DocumentNode
                .SelectNodes("//a[contains(@class,'jobsearch-result')] | //div[contains(@class,'jobsearch-result')]")
                ?.ToList() ?? [];
            foreach (var node in classNodes)
            {
                var href = node.Name == "a"
                    ? node.GetAttributeValue("href", "")
                    : node.SelectSingleNode(".//a[contains(@class,'jobsearch-result')]")?.GetAttributeValue("href", "") ?? string.Empty;
                var id = ExtractIdFromUrl(href);
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                {
                    continue;
                }

                var container = node.Name == "a" ? node : node.SelectSingleNode(".//a") ?? node;
                var title = container.SelectSingleNode(".//*[contains(@class,'title')] | .//h2 | .//h3")?.InnerText ?? string.Empty;
                var company = container.SelectSingleNode(".//*[contains(@class,'company')]")?.InnerText ?? string.Empty;
                var location = container.SelectSingleNode(".//*[contains(@class,'location')]")?.InnerText ?? string.Empty;
                var snippet = container.SelectSingleNode(".//*[contains(@class,'description') or contains(@class,'snippet')]")?.InnerText ?? string.Empty;
                var dateText = container.SelectSingleNode(".//time")?.GetAttributeValue("datetime", "")
                    ?? container.SelectSingleNode(".//*[contains(@class,'date')]")?.InnerText
                    ?? string.Empty;

                var jobUrl = ResolveJobUrl(href, id);
                results.Add(new JobSearchResult(
                    id,
                    "Jobindex",
                    Clean(title),
                    Clean(company),
                    Clean(location),
                    ParseDate(dateText),
                    jobUrl,
                    jobUrl,
                    Clean(snippet, maxLength: 500)
                ));
            }
        }

        if (results.Count == 0)
        {
            var anchors = docNode.DocumentNode
                .SelectNodes("//a[contains(@href,'vis-job') or contains(@href,'jobsoegning')]")
                ?.ToList() ?? [];
            foreach (var node in anchors)
            {
                var href = node.GetAttributeValue("href", "");
                var id = ExtractIdFromUrl(href);
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                {
                    continue;
                }

                var title = node.InnerText.Trim();
                var company = node.SelectSingleNode(".//*[contains(@class,'company')]")?.InnerText ?? string.Empty;
                var location = node.SelectSingleNode(".//*[contains(@class,'location')]")?.InnerText ?? string.Empty;
                var snippet = node.SelectSingleNode(".//*[contains(@class,'description') or contains(@class,'snippet')]")?.InnerText ?? string.Empty;
                var dateText = node.SelectSingleNode(".//time")?.GetAttributeValue("datetime", "") ?? string.Empty;

                var jobUrl = ResolveJobUrl(href, id);
                results.Add(new JobSearchResult(
                    id,
                    "Jobindex",
                    Clean(title),
                    Clean(company),
                    Clean(location),
                    ParseDate(dateText),
                    jobUrl,
                    jobUrl,
                    Clean(snippet, maxLength: 500)
                ));
            }
        }

        return results;
    }

    private static JobDetail? ParseDetailPage(string html, string jobId, string url)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var title = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText
            ?? doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", "")
            ?? string.Empty;

        var company = doc.DocumentNode.SelectSingleNode("//*[contains(@class,'company')]")?.InnerText
            ?? doc.DocumentNode.SelectSingleNode("//meta[@property='og:site_name']")?.GetAttributeValue("content", "")
            ?? string.Empty;

        var location = doc.DocumentNode.SelectSingleNode("//*[contains(@class,'location')]")?.InnerText ?? string.Empty;

        var descriptionNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'jobad_content')]")
            ?? doc.DocumentNode.SelectSingleNode("//*[contains(@class,'jobad-description')]")
            ?? doc.DocumentNode.SelectSingleNode("//*[contains(@id,'description')]")
            ?? doc.DocumentNode.SelectSingleNode("//article");

        var description = descriptionNode?.InnerText ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return new JobDetail
        {
            Id = jobId,
            Title = Clean(title),
            Company = Clean(company),
            Location = Clean(location),
            Url = url,
            ApplyUrl = url,
            FullDescription = Clean(description, maxLength: 20000),
        };
    }

    private static string FirstAttribute(HtmlNode node, params string[] names)
    {
        foreach (var name in names)
        {
            var value = node.GetAttributeValue(name, "");
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }
        return string.Empty;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }
        return string.Empty;
    }

    private static string GetJsonProperty(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var prop))
            {
                return prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? string.Empty : prop.ToString();
            }
        }
        return string.Empty;
    }

    private static string ExtractIdFromUrl(string? href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return string.Empty;
        }

        var match = JobIdRegex().Match(href);
        return match.Success ? match.Value : string.Empty;
    }

    private static string ResolveJobUrl(string? scrapedHref, string jobId)
    {
        if (!string.IsNullOrWhiteSpace(scrapedHref))
        {
            if (Uri.TryCreate(scrapedHref, UriKind.Absolute, out var absolute)
                && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
            {
                return absolute.ToString();
            }

            if (Uri.TryCreate(new Uri(SiteBaseUrl), scrapedHref, out var resolved))
            {
                return resolved.ToString();
            }
        }

        return BuildJobUrl(jobId);
    }

    private static string BuildJobUrl(string jobId)
    {
        return $"{SiteBaseUrl}/vis-job/{jobId}";
    }

    public static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = WebUtility.HtmlDecode(value).Trim();
        text = WhitespaceRegex().Replace(text, " ");

        var lower = text.ToLowerInvariant();
        if (lower.Contains("i dag") || lower.Contains("idag") || lower.Contains("today"))
        {
            return DateTime.UtcNow.Date;
        }

        if (lower.Contains("i går") || lower.Contains("igår") || lower.Contains("yesterday"))
        {
            return DateTime.UtcNow.Date.AddDays(-1);
        }

        var relative = RelativeDaysRegex().Match(lower);
        if (relative.Success && int.TryParse(relative.Groups["days"].Value, out var daysAgo))
        {
            return DateTime.UtcNow.Date.AddDays(-Math.Abs(daysAgo));
        }

        var cleaned = LeadingDatePrefixRegex().Replace(text, string.Empty).Trim();

        CultureInfo[] cultures = [DanishCulture, CultureInfo.InvariantCulture];
        foreach (var culture in cultures)
        {
            if (DateTime.TryParseExact(
                    cleaned,
                    DanishDateFormats,
                    culture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal,
                    out var exact))
            {
                return Normalize(exact);
            }

            if (DateTime.TryParse(
                    cleaned,
                    culture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal,
                    out var parsed))
            {
                return Normalize(parsed);
            }
        }

        return null;

        static DateTime Normalize(DateTime parsed)
        {
            if (parsed.Year == 1)
            {
                parsed = new DateTime(DateTime.UtcNow.Year, parsed.Month, parsed.Day, 0, 0, 0, DateTimeKind.Utc);
            }

            var utc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

            return utc.Date > DateTime.UtcNow.Date ? utc.AddYears(-1) : utc;
        }
    }

    internal static string Clean(string? value, int maxLength = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decoded = WebUtility.HtmlDecode(value);
        var collapsed = WhitespaceRegex().Replace(decoded, " ").Trim();
        if (maxLength > 0 && collapsed.Length > maxLength)
        {
            collapsed = collapsed[..maxLength];
        }
        return collapsed;
    }

    private async Task ThrottleAsync(CancellationToken cancellationToken)
    {
        var minIntervalMs = Math.Max(100.0, 1000.0 / Math.Max(0.1, _options.Value.Jobindex.RateLimitPerSecond));
        int waitMs;
        lock (RateLimitGate)
        {
            var elapsedMs = (DateTime.UtcNow - _lastRequestAtUtc).TotalMilliseconds;
            waitMs = elapsedMs < minIntervalMs ? (int)(minIntervalMs - elapsedMs) + 10 : 0;
            _lastRequestAtUtc = DateTime.UtcNow.AddMilliseconds(waitMs);
        }
        if (waitMs > 0)
        {
            await Task.Delay(waitMs, cancellationToken);
        }
    }
}
