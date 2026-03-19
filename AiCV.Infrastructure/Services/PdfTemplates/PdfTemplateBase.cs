namespace AiCV.Infrastructure.Services.PdfTemplates;

public abstract partial class PdfTemplateBase(IWebHostEnvironment env, IStringLocalizer<AicvResources> localizer) : IPdfTemplateBuilder
{
    protected readonly IWebHostEnvironment _env = env;
    protected readonly IStringLocalizer<AicvResources> _localizer = localizer;

    protected string _primaryColor = "#2c7be5";
    protected string _primaryDark = "#1e5fae";
    protected string _accentColor = "#10b981";
    protected string _textDark = "#1f2937";
    protected string _textMedium = "#4b5563";
    protected string _backgroundLight = "#f9fafb";
    protected string _borderColor = "#e5e7eb";

    protected const string CheckmarkSvgPath = "M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z";

    public abstract void ComposeHeader(IContainer container, CandidateProfile profile);
    public abstract void ComposePageOne(
        IContainer container,
        CandidateProfile profile,
        float fontSize
    );
    public abstract void ComposePageTwo(
        IContainer container,
        CandidateProfile profile,
        float fontSize
    );
    public abstract void ComposePageThree(
        IContainer container,
        CandidateProfile profile,
        float fontSize
    );
    public abstract void ComposeCoverLetter(
        IContainer container,
        string letterContent,
        CandidateProfile profile,
        float fontSize
    );

    public int GetPageCount(byte[] pdfBytes)
    {
        try
        {
            var text = Encoding.Default.GetString(pdfBytes);
            var matches = PageTypeRegex().Matches(text);
            return matches.Count;
        }
        catch
        {
            return 1;
        }
    }

    protected virtual void SectionTitle(ColumnDescriptor column, string title)
    {
        column
            .Item()
            .PaddingBottom(0.3f, Unit.Centimetre)
            .PaddingTop(0.3f, Unit.Centimetre)
            .Row(row =>
            {
                row.AutoItem()
                    .Text(title.ToUpper())
                    .FontSize(12)
                    .Bold()
                    .FontColor(_primaryDark)
                    .LetterSpacing(0.06f);
            });
    }

    protected void ComposeContactRow(
        TextDescriptor t,
        CandidateProfile profile,
        bool isUpper,
        string headerTextCol
    )
    {
        bool first = true;
        void AddPart(string label, string? value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            if (!first)
                t.Span(" | ");
            var labelText = _localizer[label].Value;
            if (isUpper)
                labelText = labelText.ToUpper();
            t.Span($"{labelText} ");
            var rawValue = value ?? "";
            var cleanValue = HtmlTagRegex().Replace(rawValue, "").Trim();
            if (cleanValue.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                cleanValue = cleanValue[7..];
            var displayValue = isUpper ? rawValue.ToUpper() : rawValue;
            if (label == "EmailLabel")
            {
                ComposeMarkdownText(
                    t,
                    $"<a href='mailto:{cleanValue}'>{displayValue}</a>",
                    headerTextCol
                );
            }
            else
            {
                ComposeMarkdownText(t, displayValue, headerTextCol);
            }
            first = false;
        }
        AddPart("EmailLabel", profile.Email);
        AddPart("PhoneLabel", profile.PhoneNumber);
        AddPart("LocationLabel", profile.Location);
    }

    protected void ComposeLinkRow(
        TextDescriptor t,
        CandidateProfile profile,
        bool isUpper,
        string headerTextCol
    )
    {
        bool first = true;
        void AddLink(string label, string? value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            if (!first)
                t.Span(" | ");
            var labelText = _localizer[label].Value;
            if (isUpper)
                labelText = labelText.ToUpper();
            t.Span($"{labelText} ");
            var rawValue = value ?? "";
            var pureUrl = HtmlTagRegex().Replace(rawValue, "").Trim();
            var displayValue = isUpper ? rawValue.ToUpper() : rawValue;
            ComposeMarkdownText(t, $"<a href='{pureUrl}'>{displayValue}</a>", headerTextCol);
            first = false;
        }
        AddLink("LinkedInLabel", profile.LinkedInUrl);
        AddLink("GitHubLabel", profile.PortfolioUrl);
    }

    protected void ComposeMarkdownText(TextDescriptor t, string? content, string? color = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;
        FormatHtmlToText(
            t,
            PreprocessHtml(content, isBlock: false),
            isBlock: false,
            fallbackColor: color
        );
    }

    protected void ComposeHtmlContent(
        ColumnDescriptor column,
        string? input,
        float fontSize,
        string fontColor,
        string? bullet = null,
        float lineHeight = 1.2f,
        float paragraphSpacing = 0f,
        bool preserveParagraphBreaks = false
    )
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        var preprocessed = PreprocessHtml(input, isBlock: true, bullet, preserveParagraphBreaks);
        var parts = preprocessed.Split(["[[HR]]"], StringSplitOptions.None);

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (!string.IsNullOrWhiteSpace(part))
            {
                var checkmarkParts = part.Split(["[[CHECKMARK]]"], StringSplitOptions.None);
                for (int j = 0; j < checkmarkParts.Length; j++)
                {
                    var segment = checkmarkParts[j];
                    if (j == 0)
                    {
                        if (!string.IsNullOrWhiteSpace(segment))
                        {
                            ComposeTextSegment(
                                column,
                                segment,
                                fontSize,
                                fontColor,
                                lineHeight,
                                paragraphSpacing,
                                preserveParagraphBreaks
                            );
                        }
                    }
                    else
                    {
                        column
                            .Item()
                            .Row(row =>
                            {
                                var svgXml =
                                    $"<svg viewBox=\"0 0 24 24\"><path d=\"{CheckmarkSvgPath}\" fill=\"{_accentColor}\"/></svg>";
                                row.ConstantItem(fontSize + 5)
                                    .PaddingRight(5)
                                    .PaddingTop(2)
                                    .Height(fontSize)
                                    .Svg(svgXml);
                                row.RelativeItem()
                                    .Text(t =>
                                    {
                                        t.DefaultTextStyle(s =>
                                            s.FontSize(fontSize)
                                                .FontColor(fontColor)
                                                .LineHeight(lineHeight)
                                        );
                                        t.ParagraphSpacing(paragraphSpacing);
                                        FormatHtmlToText(
                                            t,
                                            segment.Trim(),
                                            isBlock: true,
                                            fallbackColor: fontColor
                                        );
                                    });
                            });
                    }
                }
            }
            if (i < parts.Length - 1)
            {
                column
                    .Item()
                    .PaddingVertical(0.1f, Unit.Centimetre)
                    .LineHorizontal(1)
                    .LineColor(_borderColor);
            }
        }
    }

    private static void ComposeTextSegment(
        ColumnDescriptor column,
        string segment,
        float fontSize,
        string fontColor,
        float lineHeight,
        float paragraphSpacing,
        bool preserveParagraphBreaks
    )
    {
        var paragraphs = preserveParagraphBreaks
            ? segment.Split(
                ["[[PARAGRAPH]]"],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            )
            : [segment];

        for (int paragraphIndex = 0; paragraphIndex < paragraphs.Length; paragraphIndex++)
        {
            var paragraph = paragraphs[paragraphIndex];
            if (string.IsNullOrWhiteSpace(paragraph))
                continue;

            var item = column.Item();
            if (preserveParagraphBreaks && paragraphIndex < paragraphs.Length - 1)
                item = item.PaddingBottom(paragraphSpacing, Unit.Point);

            item.Text(t =>
            {
                t.DefaultTextStyle(s =>
                    s.FontSize(fontSize)
                        .FontColor(fontColor)
                        .LineHeight(lineHeight)
                );
                t.ParagraphSpacing(paragraphSpacing);
                FormatHtmlToText(t, paragraph, isBlock: true, fallbackColor: fontColor);
            });
        }
    }

    protected static void FormatHtmlToText(
        TextDescriptor textDescriptor,
        string? input,
        bool isBlock = false,
        string? fallbackColor = null,
        bool isBold = false,
        bool isItalic = false,
        bool isUnderline = false,
        string? color = null,
        string? linkUrl = null
    )
    {
        if (string.IsNullOrWhiteSpace(input))
            return;
        var regex = HtmlTagWithStyleRegex();
        int lastIndex = 0;

        foreach (Match match in regex.Matches(input))
        {
            if (match.Index > lastIndex)
            {
                var beforeText = input[lastIndex..match.Index];
                var cleanBefore = HtmlTagRegex().Replace(beforeText, string.Empty);
                if (
                    !string.IsNullOrWhiteSpace(cleanBefore)
                    || (isBlock && !string.IsNullOrEmpty(cleanBefore))
                )
                {
                    var span = !string.IsNullOrEmpty(linkUrl)
                        ? textDescriptor.Hyperlink(linkUrl, cleanBefore)
                        : textDescriptor.Span(cleanBefore);
                    ApplyStyle(
                        span,
                        isBold,
                        isItalic,
                        isUnderline,
                        color ?? (string.IsNullOrEmpty(linkUrl) ? null : fallbackColor)
                    );
                }
            }

            var tagName = match.Groups[1].Value.ToLower();
            var attributes = match.Groups[2].Value;
            var content = match.Groups[3].Value;

            bool currentBold =
                isBold
                || tagName == "strong"
                || tagName == "b"
                || tagName.StartsWith('h')
                || tagName == "code"
                || tagName == "pre";
            bool currentItalic =
                isItalic || tagName == "em" || tagName == "i" || tagName == "blockquote";
            bool currentUnderline = isUnderline || tagName == "u";
            string? currentColor = color;
            string? currentLink = linkUrl;

            if (tagName == "a")
            {
                var hrefMatch = HrefAttributeRegex().Match(attributes);
                if (hrefMatch.Success)
                {
                    currentLink =
                        hrefMatch.Groups[1].Value
                        + hrefMatch.Groups[2].Value
                        + hrefMatch.Groups[3].Value;
                }
            }

            if (!string.IsNullOrEmpty(attributes))
            {
                var styleMatch = StyleAttributeRegex().Match(attributes);
                if (styleMatch.Success)
                {
                    var styleAttr =
                        styleMatch.Groups[1].Value
                        + styleMatch.Groups[2].Value
                        + styleMatch.Groups[3].Value;
                    var colorMatch = ColorStyleRegex().Match(styleAttr);
                    if (colorMatch.Success)
                        currentColor = GetHexColor(colorMatch.Groups[1].Value.Trim());
                    var weightMatch = FontWeightStyleRegex().Match(styleAttr);
                    if (weightMatch.Success)
                    {
                        var weight = weightMatch.Groups[1].Value.Trim().ToLower();
                        if (weight == "bold" || weight == "700" || weight == "800")
                            currentBold = true;
                        else if (weight == "normal" || weight == "400")
                            currentBold = false;
                    }
                }
            }

            FormatHtmlToText(
                textDescriptor,
                content,
                isBlock,
                fallbackColor ?? color,
                currentBold,
                currentItalic,
                currentUnderline,
                currentColor,
                currentLink
            );
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < input.Length)
        {
            var remainingText = input[lastIndex..];
            var cleanRemaining = HtmlTagRegex().Replace(remainingText, string.Empty);
            if (
                !string.IsNullOrWhiteSpace(cleanRemaining)
                || (isBlock && !string.IsNullOrEmpty(cleanRemaining))
            )
            {
                var span = !string.IsNullOrEmpty(linkUrl)
                    ? textDescriptor.Hyperlink(linkUrl, cleanRemaining)
                    : textDescriptor.Span(cleanRemaining);
                ApplyStyle(
                    span,
                    isBold,
                    isItalic,
                    isUnderline,
                    color ?? (string.IsNullOrEmpty(linkUrl) ? null : fallbackColor)
                );
            }
        }
    }

    protected string PreprocessHtml(
        string? input,
        bool isBlock = true,
        string? bullet = null,
        bool preserveParagraphBreaks = false
    )
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        string pText = WebUtility.HtmlDecode(input ?? "");
        pText = pText.Replace("mailto:", "", StringComparison.OrdinalIgnoreCase);

        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        pText = Markdown.ToHtml(pText, pipeline);

        if (!isBlock)
        {
            pText = pText.Trim();
            if (
                pText.StartsWith("<p>", StringComparison.OrdinalIgnoreCase)
                && pText.EndsWith("</p>", StringComparison.OrdinalIgnoreCase)
                && pText.IndexOf("<p>", 3, StringComparison.OrdinalIgnoreCase) == -1
            )
            {
                pText = pText[3..^4];
            }
        }

        pText = HrTagRegex().Replace(pText, "[[HR]]");
        const string checkmarkPlaceholder = "[[CHECKMARK]]";

        if (pText.Contains("<li>", StringComparison.OrdinalIgnoreCase))
        {
            pText = LiWithNestedPRegex().Replace(pText, "<li>$1</li>");
            string bRep = bullet ?? "";
            if (!string.IsNullOrEmpty(bullet))
            {
                if (bullet.Contains('\u25B8'))
                    bRep = $"<span style='color:{_primaryColor}'>\u25B8</span> ";
                else if (bullet.Contains('\u2713'))
                    bRep = checkmarkPlaceholder;
            }
            pText = ListTagRegex().Replace(pText, "");
            pText = LiCloseWithWhitespaceRegex().Replace(pText, "");
            pText = LiOpenWithWhitespaceRegex().Replace(pText, "\n" + bRep);
            pText = pText.Trim().Replace("&#8226;", "• ");
            pText = pText.Replace("\u2713", checkmarkPlaceholder);
        }

        pText = BrTagRegex().Replace(pText, "\n");
        pText = preserveParagraphBreaks
            ? ParagraphBreakRegex().Replace(pText, "$1[[PARAGRAPH]]$2")
            : BlockGapsRegex().Replace(pText, "$1$2");
        pText = preserveParagraphBreaks
            ? MultipleParagraphBreakRegex().Replace(pText, "[[PARAGRAPH]]")
            : MultipleNewlineRegex().Replace(pText, "\n");
        pText = pText.Replace("\u2713", checkmarkPlaceholder);
        pText = pText.Replace("\u25B8", $"<span style='color:{_primaryColor}'>\u25B8</span>");

        return pText.Trim();
    }

    protected static void ApplyStyle(
        TextSpanDescriptor span,
        bool bold,
        bool italic,
        bool underline,
        string? color
    )
    {
        if (bold)
            span.Bold();
        if (italic)
            span.Italic();
        if (underline)
            span.Underline();
        if (!string.IsNullOrEmpty(color))
            span.FontColor(color);
    }

    protected string CalculateDuration(DateTime? start, DateTime? end, bool isCurrentRole = false)
    {
        return CvHelpers.CalculateDuration(start, end, isCurrentRole, _localizer);
    }

    protected static string? GetHexColor(string? colorName)
    {
        if (string.IsNullOrWhiteSpace(colorName))
            return null;
        colorName = colorName.Trim().TrimEnd(';');
        if (NamedColors.TryGetValue(colorName, out var hexName))
            return hexName;
        if (colorName.StartsWith('#'))
            return HexColorRegex().IsMatch(colorName[1..]) ? colorName : null;
        return HexColorRegex().IsMatch(colorName) ? "#" + colorName : null;
    }

    protected static readonly Dictionary<string, string> NamedColors = new(
        StringComparer.OrdinalIgnoreCase
    )
    {
        { "black", "#000000" },
        { "white", "#FFFFFF" },
        { "red", "#FF0000" },
        { "lime", "#00FF00" },
        { "blue", "#0000FF" },
        { "yellow", "#FFFF00" },
        { "cyan", "#00FFFF" },
        { "magenta", "#FF00FF" },
        { "silver", "#C0C0C0" },
        { "gray", "#808080" },
        { "grey", "#808080" },
        { "maroon", "#800000" },
        { "olive", "#808000" },
        { "green", "#008000" },
        { "purple", "#800080" },
        { "teal", "#008080" },
        { "navy", "#000080" },
        { "orange", "#FFA500" },
    };

    [GeneratedRegex(@"<[a-zA-Z/][^>]*>", RegexOptions.IgnoreCase)]
    protected static partial Regex HtmlTagRegex();

    [GeneratedRegex("^[0-9a-fA-F]{3,8}$")]
    protected static partial Regex HexColorRegex();

    [GeneratedRegex(@"/Type\s*/Page\b", RegexOptions.IgnoreCase)]
    protected static partial Regex PageTypeRegex();

    [GeneratedRegex(@"href\s*=\s*(?:""([^""]*)""|'([^']*)'|([^""'\s>]+))", RegexOptions.IgnoreCase)]
    protected static partial Regex HrefAttributeRegex();

    [GeneratedRegex(
        @"style\s*=\s*(?:""([^""]*)""|'([^']*)'|([^""'\s>]+))",
        RegexOptions.IgnoreCase
    )]
    protected static partial Regex StyleAttributeRegex();

    [GeneratedRegex(@"color\s*:\s*([^;""'\s]+)", RegexOptions.IgnoreCase)]
    protected static partial Regex ColorStyleRegex();

    [GeneratedRegex(@"font-weight\s*:\s*([^;]+)", RegexOptions.IgnoreCase)]
    protected static partial Regex FontWeightStyleRegex();

    [GeneratedRegex(@"font-style\s*:\s*([^;]+)", RegexOptions.IgnoreCase)]
    protected static partial Regex FontStyleStyleRegex();

    [GeneratedRegex(@"<ul>|</ul>|<ol>|</ol>", RegexOptions.IgnoreCase)]
    protected static partial Regex ListTagRegex();

    [GeneratedRegex(@"<br.*?>", RegexOptions.IgnoreCase)]
    protected static partial Regex BrTagRegex();

    [GeneratedRegex(@"<hr\s*/?>", RegexOptions.IgnoreCase)]
    protected static partial Regex HrTagRegex();

    [GeneratedRegex(
        @"\b((?:https?://|www\.)[^\s<]*[^\s<.,!?;:]|[a-zA-Z0-9._%+-]+@(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,})\b",
        RegexOptions.IgnoreCase
    )]
    protected static partial Regex AutoLinkRegex();

    [GeneratedRegex(@"</li>\s*", RegexOptions.IgnoreCase)]
    protected static partial Regex LiCloseWithWhitespaceRegex();

    [GeneratedRegex(@"\s*<li>", RegexOptions.IgnoreCase)]
    protected static partial Regex LiOpenWithWhitespaceRegex();

    [GeneratedRegex(
        @"<li>\s*<p>(.*?)</p>\s*</li>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline
    )]
    protected static partial Regex LiWithNestedPRegex();

    [GeneratedRegex(@"\n{2,}")]
    protected static partial Regex MultipleNewlineRegex();

    [GeneratedRegex(@"(\[\[PARAGRAPH\]\]){2,}")]
    protected static partial Regex MultipleParagraphBreakRegex();

    [GeneratedRegex(
        @"(</p>|</div>|</h1>|</h2>|h3>|</h4>|<h5>|</h6>)\s+(<p|<div>|<h1|<h2|<h3|<h4|<h5|<h6)",
        RegexOptions.IgnoreCase
    )]
    protected static partial Regex BlockGapsRegex();

    [GeneratedRegex(
        @"(</p>|</div>|</h1>|</h2>|</h3>|</h4>|</h5>|</h6>)\s*(<p|<div|<h1|<h2|<h3|<h4|<h5|<h6)",
        RegexOptions.IgnoreCase
    )]
    protected static partial Regex ParagraphBreakRegex();

    private static readonly Regex HtmlTagWithStyleRegexInstance = new(
        @"<(strong|b|em|i|u|span|div|p|h1|h2|h3|h4|h5|h6|a|blockquote|code|pre)(?:\s+([^>]*?))?\s*>(.+?)</\1>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline
    );

    protected static Regex HtmlTagWithStyleRegex() => HtmlTagWithStyleRegexInstance;
}
