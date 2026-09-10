namespace AiCV.Web.Components.Shared;

public static partial class CvTextFormatter
{
    private const string ChipSpanStyle =
        "display:inline-block;padding:0.15rem 0.5rem;border-radius:1rem;"
        + "background-color:var(--bg-light);border:1px solid var(--border-color);"
        + "font-size:0.9em;color:var(--text-dark);";

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

        formatted = MissingHexHashRegex().Replace(formatted, "$1#$2");
        formatted = ChipBadgeTagRegex().Replace(
            formatted,
            $"<span style=\"{ChipSpanStyle}\">$1</span>"
        );
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
        var formatted = Markdown.ToHtml(pText, pipeline).Trim();

        if (formatted.Contains("<li>", StringComparison.OrdinalIgnoreCase))
        {
            formatted = LiWithNestedPRegex().Replace(formatted, "<li>$1</li>");
        }

        formatted = MissingHexHashRegex().Replace(formatted, "$1#$2");
        formatted = ChipBadgeTagRegex().Replace(
            formatted,
            $"<span style=\"{ChipSpanStyle}\">$1</span>"
        );
        return formatted;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"<u>(.*?)</u>")]
    private static partial System.Text.RegularExpressions.Regex UnderlineRegex();

    [System.Text.RegularExpressions.GeneratedRegex(
        @"(color\s*:\s*)(?!#)([0-9a-fA-F]{3,8})\b",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase
    )]
    private static partial System.Text.RegularExpressions.Regex MissingHexHashRegex();

    [System.Text.RegularExpressions.GeneratedRegex(
        @"<li>\s*<p>(.*?)</p>\s*</li>",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase
            | System.Text.RegularExpressions.RegexOptions.Singleline
    )]
    private static partial System.Text.RegularExpressions.Regex LiWithNestedPRegex();

    [System.Text.RegularExpressions.GeneratedRegex(
        @"<(?:chip|badge)\s*>(.*?)</(?:chip|badge)>",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase
            | System.Text.RegularExpressions.RegexOptions.Singleline
    )]
    private static partial System.Text.RegularExpressions.Regex ChipBadgeTagRegex();
}
