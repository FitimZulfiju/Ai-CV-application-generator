namespace AiCV.Web.Components.Shared;

public static partial class CvTextFormatter
{
    public static string FormatSummary(string? summary)
    {
        if (string.IsNullOrEmpty(summary))
            return string.Empty;

        var pText = System.Net.WebUtility.HtmlDecode(summary ?? "");
        if (pText.Contains("&lt;") || pText.Contains("&amp;"))
            pText = System.Net.WebUtility.HtmlDecode(pText);

        if (pText.Contains("mailto:", StringComparison.OrdinalIgnoreCase))
            pText = pText.Replace("mailto:", "", StringComparison.OrdinalIgnoreCase);

        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var formatted = Markdown.ToHtml(pText, pipeline).Trim();

        if (
            formatted.StartsWith("<p>", StringComparison.OrdinalIgnoreCase)
            && formatted.EndsWith("</p>", StringComparison.OrdinalIgnoreCase)
            && formatted.IndexOf("<p>", 3, StringComparison.OrdinalIgnoreCase) == -1
        )
        {
            formatted = formatted[3..^4];
        }

        return UnderlineRegex().Replace(formatted, "<u>$1</u>");
    }

    public static string FormatDescription(string? description)
    {
        if (string.IsNullOrEmpty(description))
            return string.Empty;

        var pText = description;
        if (pText.Contains("mailto:", StringComparison.OrdinalIgnoreCase))
            pText = pText.Replace("mailto:", "", StringComparison.OrdinalIgnoreCase);

        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        var formatted = Markdown.ToHtml(pText, pipeline);

        if (formatted.Contains("<li>", StringComparison.OrdinalIgnoreCase))
        {
            formatted = LiWithNestedPRegex().Replace(formatted, "<li>$1</li>");
        }

        return formatted;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"<u>(.*?)</u>")]
    private static partial System.Text.RegularExpressions.Regex UnderlineRegex();

    [System.Text.RegularExpressions.GeneratedRegex(
        @"<li>\s*<p>(.*?)</p>\s*</li>",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase
            | System.Text.RegularExpressions.RegexOptions.Singleline
    )]
    private static partial System.Text.RegularExpressions.Regex LiWithNestedPRegex();
}
