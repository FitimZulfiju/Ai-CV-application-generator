namespace WebCV.Infrastructure.Services
{
    public partial class JobPostScraper(IHttpClientFactory httpClientFactory) : IJobPostScraper
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        [GeneratedRegex(@"\s+")]
        private static partial Regex MyRegex();

        [GeneratedRegex(@"(\r?\n\s*){3,}")]
        private static partial Regex MultipleNewlinesRegex();

        public async Task<JobPosting> ScrapeJobPostingAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL cannot be empty.", nameof(url));

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                // Add a user agent to mimic a browser
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                var html = await httpClient.GetStringAsync(url);
                
                // Use SmartReader to extract main content
                var reader = new SmartReader.Reader(url, html);
                var article = await reader.GetArticleAsync();

                // Convert to Markdown
                var config = new ReverseMarkdown.Config
                {
                    UnknownTags = ReverseMarkdown.Config.UnknownTagsOption.Bypass,
                    GithubFlavored = true,
                    RemoveComments = true,
                    SmartHrefHandling = true
                };

                var converter = new ReverseMarkdown.Converter(config);
                string markdown;

                if (article.IsReadable)
                {
                    markdown = converter.Convert(article.Content);
                }
                else
                {
                    // Fallback: Convert the entire body if SmartReader fails
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);
                    
                    // Remove script and style tags manually for fallback
                    var nodesToRemove = doc.DocumentNode.SelectNodes("//script|//style|//noscript|//iframe|//svg|//nav|//footer");
                    if (nodesToRemove != null)
                    {
                        foreach (var node in nodesToRemove)
                        {
                            node.Remove();
                        }
                    }
                    
                    markdown = converter.Convert(doc.DocumentNode.OuterHtml);
                }

                // Normalize newlines: Replace 3 or more newlines with 2 newlines (one empty row)
                markdown = MultipleNewlinesRegex().Replace(markdown, Environment.NewLine + Environment.NewLine);

                // Extract Metadata (Title, Company) using our existing logic as it handles separators well
                var docMetadata = new HtmlDocument();
                docMetadata.LoadHtml(html);
                var (title, company) = ExtractMetadata(docMetadata);

                // If our metadata extraction failed, try SmartReader's title
                if (string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(article.Title))
                {
                    title = article.Title;
                }

                // Ensure the title is at the top of the description if it's not already there
                if (!string.IsNullOrWhiteSpace(title) && !markdown.StartsWith($"# {title}", StringComparison.OrdinalIgnoreCase))
                {
                    markdown = $"# {title}{Environment.NewLine}{Environment.NewLine}{markdown}";
                }

                return new JobPosting
                {
                    Url = url,
                    Description = markdown.Trim(),
                    Title = !string.IsNullOrWhiteSpace(title) ? title : "Imported Job",
                    CompanyName = !string.IsNullOrWhiteSpace(company) ? company : string.Empty,
                    DatePosted = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to scrape job posting: {ex.Message}", ex);
            }
        }

        private static (string title, string company) ExtractMetadata(HtmlDocument doc)
        {
            string title = string.Empty;
            string company = string.Empty;

            // 1. Try Open Graph Title (often most accurate)
            var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", "").Trim();
            // 2. Try Standard Meta Title
            var metaTitle = doc.DocumentNode.SelectSingleNode("//meta[@name='title']")?.GetAttributeValue("content", "").Trim();
            // 3. Try HTML Title Tag
            var htmlTitle = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim();

            // 4. Try Open Graph Site Name for Company
            var ogSiteName = doc.DocumentNode.SelectSingleNode("//meta[@property='og:site_name']")?.GetAttributeValue("content", "").Trim();
            if (!string.IsNullOrWhiteSpace(ogSiteName))
            {
                company = ogSiteName;
            }

            // Pick the best candidate (prefer OG, then Meta, then HTML)
            var fullTitle = !string.IsNullOrWhiteSpace(ogTitle) ? ogTitle :
                            (!string.IsNullOrWhiteSpace(metaTitle) ? metaTitle : 
                            System.Net.WebUtility.HtmlDecode(htmlTitle ?? string.Empty).Trim());

            if (!string.IsNullOrWhiteSpace(fullTitle))
            {
                // We used to split by separators ("-", "|") but that often cut off the actual job title 
                // if the format was "Department | Job Title | Location".
                // It's safer to return the full title and let the user or AI clean it up.
                title = fullTitle;
            }

            return (title, company);
        }
        
        // ExtractTextFromHtml is no longer needed as we use SmartReader + ReverseMarkdown directly
    }
}
