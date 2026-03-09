namespace AiCV.Web.Components.Shared;

public partial class CvPreview
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    [Parameter]
    public CvTemplate Template { get; set; } = CvTemplate.Professional;

    private string GetTemplateClass() =>
        Template switch
        {
            CvTemplate.Modern => "cv-modern",
            CvTemplate.Minimalist => "cv-minimalist",
            _ => "cv-professional",
        };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Only scale content on first render to avoid infinite render loop
        if (firstRender && Profile != null)
        {
            // Small delay to ensure DOM is fully calculated
            await Task.Delay(100);
            await JSRuntime.InvokeVoidAsync("cvScaler.fitContentToPages");
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    public static string FormatSummary(string? summary)
    {
        if (string.IsNullOrEmpty(summary))
            return string.Empty;

        // Decode entities (robustly handle multiple levels of encoding if present)
        var pText = System.Net.WebUtility.HtmlDecode(summary ?? "");
        if (pText.Contains("&lt;") || pText.Contains("&amp;"))
            pText = System.Net.WebUtility.HtmlDecode(pText);

        // Strip mailto: from input text before processing to avoid display issues
        if (pText.Contains("mailto:", StringComparison.OrdinalIgnoreCase))
            pText = pText.Replace("mailto:", "", StringComparison.OrdinalIgnoreCase);

        // Run Markdig for full markdown support
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var formatted = Markdown.ToHtml(pText, pipeline).Trim();

        // If it's single-line/inline (FormatSummary is used for headers too)
        // strip the wrapping <p> tags if it's a single paragraph
        if (
            formatted.StartsWith("<p>", StringComparison.OrdinalIgnoreCase)
            && formatted.EndsWith("</p>", StringComparison.OrdinalIgnoreCase)
            && formatted.IndexOf("<p>", 3, StringComparison.OrdinalIgnoreCase) == -1
        )
        {
            formatted = formatted[3..^4];
        }

        // Handle underline
        return UnderlineRegex().Replace(formatted, "<u>$1</u>");
    }

    public static string FormatDescription(string? description)
    {
        if (string.IsNullOrEmpty(description))
            return string.Empty;

        // Strip mailto: from input text before processing
        var pText = description;
        if (pText.Contains("mailto:", StringComparison.OrdinalIgnoreCase))
            pText = pText.Replace("mailto:", "", StringComparison.OrdinalIgnoreCase);

        // Run Markdig for full markdown support
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var formatted = Markdown.ToHtml(pText, pipeline);

        return formatted;
    }

    private string CalculateDuration(DateTime? start, DateTime? end, bool isCurrentRole = false)
    {
        if (!start.HasValue || isCurrentRole)
            return "";

        var endDate = end ?? DateTime.Now;
        var totalMonths =
            ((endDate.Year - start.Value.Year) * 12) + endDate.Month - start.Value.Month + 1;

        var years = totalMonths / 12;
        var months = totalMonths % 12;

        var parts = new List<string>();
        if (years > 0)
        {
            var yearKey = years > 1 ? "Years" : "Year";
            parts.Add($"{years} {Localizer[yearKey]}");
        }
        if (months > 0)
        {
            var monthKey = months > 1 ? "Months" : "Month";
            parts.Add($"{months} {Localizer[monthKey]}");
        }

        return string.Join(" ", parts);
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"<u>(.*?)</u>")]
    private static partial System.Text.RegularExpressions.Regex UnderlineRegex();
}
