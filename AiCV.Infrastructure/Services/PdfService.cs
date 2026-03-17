namespace AiCV.Infrastructure.Services;

public partial class PdfService(IWebHostEnvironment env, IStringLocalizer<AicvResources> localizer)
    : IPdfService
{
    private readonly IWebHostEnvironment _env = env;
    private readonly IStringLocalizer<AicvResources> _localizer = localizer;

    // Define colors
    // Define colors from CSS
    // Base colors (fallback to Professional)
    private string _primaryColor = "#2c7be5";
    private string _primaryDark = "#1e5fae";
    private string _accentColor = "#10b981";
    private string _textDark = "#1f2937";
    private string _textMedium = "#4b5563";
    private string _backgroundLight = "#f9fafb";
    private string _borderColor = "#e5e7eb";

    private void SetTemplateColors(CvTemplate template)
    {
        switch (template)
        {
            case CvTemplate.Modern:
                _primaryColor = "#2c3e50";
                _primaryDark = "#1a252f";
                _accentColor = "#e67e22";
                _textDark = "#2c3e50";
                _textMedium = "#4b5563";
                _backgroundLight = "#f8f9fa";
                _borderColor = "#dee2e6";
                break;
            case CvTemplate.Minimalist:
                _primaryColor = "#333333";
                _primaryDark = "#111111";
                _accentColor = "#777777";
                _textDark = "#111111";
                _textMedium = "#444444";
                _backgroundLight = "#ffffff";
                _borderColor = "#eeeeee";
                break;
            default:
                _primaryColor = "#2c7be5";
                _primaryDark = "#1e5fae";
                _accentColor = "#10b981";
                _textDark = "#1f2937";
                _textMedium = "#4b5563";
                _backgroundLight = "#f9fafb";
                _borderColor = "#e5e7eb";
                break;
        }
    }

    private static readonly Dictionary<string, string> NamedColors = new(
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

    private static string? GetHexColor(string? colorName)
    {
        if (string.IsNullOrWhiteSpace(colorName))
            return null;

        // Strip trailing semicolon and trim whitespace
        colorName = colorName.Trim().TrimEnd(';');

        // Check named colors first
        if (NamedColors.TryGetValue(colorName, out var hexName))
            return hexName;

        // If it starts with #, validate and return
        if (colorName.StartsWith('#'))
        {
            var rawHex = colorName[1..];
            if (HexColorRegex().IsMatch(rawHex))
                return colorName;

            return null;
        }

        // Fallback: If it's a valid hex without #, add #
        if (HexColorRegex().IsMatch(colorName))
            return "#" + colorName;

        return null;
    }

    public Task<byte[]> GenerateCvAsync(CandidateProfile profile, CvTemplate template)
    {
        SetTemplateColors(template);

        // Smart Sizing: Find optimal font size for each page INDEPENDENTLY
        float[] fontSizes =
        [
            14f,
            13.75f,
            13.5f,
            13.25f,
            13f,
            12.75f,
            12.5f,
            12.25f,
            12f,
            11.75f,
            11.5f,
            11.25f,
            11f,
            10.75f,
            10.5f,
            10.25f,
            10f,
            9.5f,
            9f,
            8.5f,
            8f,
        ];

        // 1. Find optimal font size for Page 1 (Header + Summary + Skills)
        float page1Size = 8f; // Default fallback
        foreach (var size in fontSizes)
        {
            var p1Doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.75f, Unit.Centimetre);
                    page.DefaultTextStyle(x =>
                        x.FontSize(size).FontFamily("Lato").FontColor(_textDark)
                    );
                    page.Header().ShowOnce().Element(c => ComposeHeader(c, profile, template));
                    page.Content().Element(c => ComposePageOne(c, profile, size, template));
                });
            });
            if (GetPageCount(p1Doc.GeneratePdf()) <= 1)
            {
                page1Size = size;
                break; // Found largest that fits
            }
        }

        // 2. Find optimal font size for Page 2 (Work Experience) - expanded range
        float[] page2FontSizes =
        [
            16f,
            15.75f,
            15.5f,
            15.25f,
            15f,
            14.75f,
            14.5f,
            14.25f,
            14f,
            13.75f,
            13.5f,
            13.25f,
            13f,
            12.75f,
            12.5f,
            12.25f,
            12f,
            11.75f,
            11.5f,
            11.25f,
            11f,
            10.75f,
            10.5f,
            10.25f,
            10f,
            9.5f,
            9f,
            8.5f,
            8f,
        ];
        float page2Size = 8f; // Default fallback
        foreach (var size in page2FontSizes)
        {
            var p2Doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.75f, Unit.Centimetre);
                    page.DefaultTextStyle(x =>
                        x.FontSize(size).FontFamily("Lato").FontColor(_textDark)
                    );
                    page.Content().Element(c => ComposePageTwo(c, profile, size, template));
                });
            });
            if (GetPageCount(p2Doc.GeneratePdf()) <= 1)
            {
                page2Size = size;
                break; // Found largest that fits
            }
        }

        // 3. Use Page 2's size for Page 3 (or could make it independent too)
        float page3Size = page2Size;

        // 4. Generate final document with independent font sizes per page
        var document = Document.Create(container =>
        {
            // Page 1
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0.75f, Unit.Centimetre);
                page.DefaultTextStyle(x =>
                    x.FontSize(page1Size).FontFamily("Lato").FontColor(_textDark)
                );
                page.Header().Element(c => ComposeHeader(c, profile, template));
                page.Content().Element(c => ComposePageOne(c, profile, page1Size, template));
            });

            // Page 2
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0.75f, Unit.Centimetre);
                page.DefaultTextStyle(x =>
                    x.FontSize(page2Size).FontFamily("Lato").FontColor(_textDark)
                );
                page.Content().Element(c => ComposePageTwo(c, profile, page2Size, template));
            });

            // Page 3+
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0.75f, Unit.Centimetre);
                page.DefaultTextStyle(x =>
                    x.FontSize(page3Size).FontFamily("Lato").FontColor(_textDark)
                );
                page.Content().Element(c => ComposePageThree(c, profile, page3Size, template));
            });
        });

        byte[] pdfBytes = document.GeneratePdf();
        return Task.FromResult(pdfBytes);
    }

    public Task<byte[]> GenerateCoverLetterAsync(
        string letterContent,
        CandidateProfile profile,
        string jobTitle,
        string companyName,
        CvTemplate template
    )
    {
        SetTemplateColors(template);

        // Smart Sizing Loop: Try fonts from 12 down to 8 to fit in 1 page
        float[] fontSizes = [12f, 11.5f, 11f, 10.5f, 10f, 9.5f, 9f, 8.5f, 8f];
        byte[] pdfBytes = [];

        foreach (var size in fontSizes)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.75f, Unit.Centimetre);
                    page.DefaultTextStyle(x =>
                        x.FontSize(size).FontFamily("Lato").FontColor(_textDark)
                    );

                    page.Header().ShowOnce().Element(c => ComposeHeader(c, profile, template));

                    page.Content()
                        .Column(col =>
                        {
                            // Body with left border and background (matching .summary style)
                            col.Item().PaddingTop(0.8f, Unit.Centimetre);

                            // Wrap the entire letter content in a styled container (thinner: 1.5pt)
                            col.Item()
                                .Background(_backgroundLight)
                                .BorderLeft(1.5f)
                                .BorderColor(
                                    template == CvTemplate.Minimalist ? _borderColor : _primaryColor
                                )
                                .CornerRadius(5)
                                .Padding(10)
                                .Column(letterCol =>
                                {
                                    // Main content
                                    if (!string.IsNullOrWhiteSpace(letterContent))
                                    {
                                        ComposeHtmlContent(
                                            letterCol,
                                            letterContent,
                                            size,
                                            _textDark
                                        );
                                    }
                                    else
                                    {
                                        letterCol.Item().Text("No content provided.").Italic();
                                    }
                                });
                        });
                });
            });

            pdfBytes = document.GeneratePdf();
            if (GetPageCount(pdfBytes) <= 1)
            {
                // Found the largest font that fits on 1 page!
                return Task.FromResult(pdfBytes);
            }
        }

        return Task.FromResult(pdfBytes!);
    }

    private void ComposeHeader(IContainer container, CandidateProfile profile, CvTemplate template)
    {
        bool showPhoto =
            profile.ShowProfilePicture && !string.IsNullOrEmpty(profile.ProfilePictureUrl);

        var headerBg = template == CvTemplate.Minimalist ? "#ffffff" : _primaryColor;
        var headerTextCol = template == CvTemplate.Minimalist ? _textDark : "#ffffff";
        var accentCol = template == CvTemplate.Minimalist ? _borderColor : _accentColor;
        var titleTextCol =
            template == CvTemplate.Minimalist
                ? _textMedium
                : (template == CvTemplate.Modern ? _accentColor : "#F5F5F5");

        container.Column(c =>
        {
            if (template == CvTemplate.Minimalist)
            {
                c.Item()
                    .BorderBottom(0.08f, Unit.Centimetre)
                    .BorderColor(accentCol)
                    .Background(headerBg)
                    .PaddingVertical(0.75f, Unit.Centimetre)
                    .PaddingHorizontal(0.75f, Unit.Centimetre)
                    .Column(col =>
                    {
                        if (showPhoto)
                        {
                            var webRootPath =
                                _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                            var path = Path.Combine(
                                webRootPath,
                                profile.ProfilePictureUrl!.TrimStart('/', '\\')
                            );
                            if (File.Exists(path))
                            {
                                col.Item()
                                    .AlignCenter()
                                    .PaddingBottom(0.5f, Unit.Centimetre)
                                    .Width(3, Unit.Centimetre)
                                    .Height(3, Unit.Centimetre)
                                    .Element(e =>
                                    {
                                        e.Background("#ffffff")
                                            .CornerRadius(1.5f, Unit.Centimetre)
                                            .Border(2)
                                            .BorderColor("#eeeeee")
                                            .Image(path)
                                            .FitArea();
                                    });
                            }
                        }

                        // Text block for Minimalist (Centered)
                        col.Item()
                            .AlignCenter()
                            .Text(t =>
                            {
                                t.AlignCenter();
                                t.DefaultTextStyle(x =>
                                    x.FontSize(28)
                                        .Bold()
                                        .FontColor(headerTextCol)
                                        .LetterSpacing(0.2f)
                                );
                                ComposeMarkdownText(
                                    t,
                                    (profile.FullName ?? "").ToUpper(),
                                    headerTextCol
                                );
                            });

                        col.Item()
                            .PaddingTop(0.1f, Unit.Centimetre)
                            .AlignCenter()
                            .Text(t =>
                            {
                                t.AlignCenter();
                                t.DefaultTextStyle(x =>
                                    x.FontSize(10.5f).FontColor(titleTextCol).LetterSpacing(0.02f)
                                );
                                ComposeMarkdownText(
                                    t,
                                    (profile.Title ?? "").ToUpper(),
                                    titleTextCol
                                );
                            });

                        col.Item()
                            .PaddingTop(0.3f, Unit.Centimetre)
                            .AlignCenter()
                            .Text(t =>
                            {
                                t.AlignCenter();
                                t.DefaultTextStyle(x =>
                                    x.FontColor(headerTextCol).FontSize(9).LetterSpacing(0.05f)
                                );
                                ComposeContactRow(t, profile, true, headerTextCol);
                            });

                        col.Item()
                            .AlignCenter()
                            .Text(t =>
                            {
                                t.AlignCenter();
                                t.DefaultTextStyle(x =>
                                    x.FontColor(headerTextCol).FontSize(9).LetterSpacing(0.05f)
                                );
                                ComposeLinkRow(t, profile, true, headerTextCol);
                            });

                        if (!string.IsNullOrWhiteSpace(profile.Tagline))
                        {
                            col.Item()
                                .PaddingTop(0.2f, Unit.Centimetre)
                                .PaddingBottom(0.2f, Unit.Centimetre)
                                .LineHorizontal(0.5f)
                                .LineColor(titleTextCol);
                            col.Item()
                                .AlignCenter()
                                .Text(t =>
                                {
                                    t.AlignCenter();
                                    t.DefaultTextStyle(x =>
                                        x.FontColor(headerTextCol).FontSize(9.5f)
                                    );
                                    ComposeMarkdownText(t, profile.Tagline, headerTextCol);
                                });
                        }
                    });
            }
            else // Modern or Professional
            {
                c.Item()
                    .CornerRadius(8)
                    .BorderBottom(0.08f, Unit.Centimetre)
                    .BorderColor(accentCol)
                    .Background(headerBg)
                    .PaddingVertical(0.75f, Unit.Centimetre)
                    .PaddingHorizontal(0.75f, Unit.Centimetre)
                    .Row(row =>
                    {
                        float photoSize = (template == CvTemplate.Modern) ? 2.2f : 2.5f;
                        float sideWidth = photoSize + 0.5f;

                        if (showPhoto)
                        {
                            // Left Column (Photo)
                            row.ConstantItem(sideWidth, Unit.Centimetre)
                                .Element(e =>
                                {
                                    var webRootPath =
                                        _env.WebRootPath
                                        ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                                    var path = Path.Combine(
                                        webRootPath,
                                        profile.ProfilePictureUrl!.TrimStart('/', '\\')
                                    );
                                    if (File.Exists(path))
                                    {
                                        e.AlignMiddle()
                                            .AlignLeft()
                                            .Width(photoSize, Unit.Centimetre)
                                            .Height(photoSize, Unit.Centimetre)
                                            .Element(inner =>
                                            {
                                                inner
                                                    .Background("#ffffff")
                                                    .CornerRadius(photoSize / 2, Unit.Centimetre)
                                                    .Border(2)
                                                    .BorderColor("#ffffff")
                                                    .Image(path)
                                                    .FitArea();
                                            });
                                    }
                                });
                        }

                        // Center Column (Text Content - Truly page-centered)
                        row.RelativeItem()
                            .AlignCenter()
                            .Column(col =>
                            {
                                // Name
                                col.Item()
                                    .AlignCenter()
                                    .Text(t =>
                                    {
                                        t.AlignCenter();
                                        t.DefaultTextStyle(x =>
                                            x.FontSize(
                                                    template == CvTemplate.Modern
                                                        ? (showPhoto ? 28 : 32)
                                                        : (showPhoto ? 24 : 26)
                                                )
                                                .Bold()
                                                .FontColor(headerTextCol)
                                                .LetterSpacing(-0.02f)
                                        );
                                        ComposeMarkdownText(
                                            t,
                                            profile.FullName ?? "",
                                            headerTextCol
                                        );
                                    });

                                // Title
                                col.Item()
                                    .PaddingTop(0.1f, Unit.Centimetre)
                                    .AlignCenter()
                                    .Text(t =>
                                    {
                                        t.AlignCenter();
                                        var title = profile.Title ?? "";
                                        if (template == CvTemplate.Modern)
                                            title = title.ToUpper();

                                        t.DefaultTextStyle(x =>
                                        {
                                            float titleFontSize =
                                                template == CvTemplate.Modern
                                                    ? (showPhoto ? 8.5f : 9.5f)
                                                    : (showPhoto ? 10f : 11f);

                                            var s = x.FontSize(titleFontSize)
                                                .FontColor(titleTextCol)
                                                .LetterSpacing(0.02f);
                                            return template == CvTemplate.Modern ? s.Bold() : s;
                                        });
                                        ComposeMarkdownText(t, title, titleTextCol);
                                    });

                                // Contact
                                col.Item()
                                    .PaddingTop(0.3f, Unit.Centimetre)
                                    .AlignCenter()
                                    .Text(t =>
                                    {
                                        t.AlignCenter();
                                        t.DefaultTextStyle(x =>
                                            x.FontColor(headerTextCol).FontSize(showPhoto ? 8f : 9f)
                                        );
                                        ComposeContactRow(t, profile, false, headerTextCol);
                                    });

                                col.Item()
                                    .AlignCenter()
                                    .Text(t =>
                                    {
                                        t.AlignCenter();
                                        t.DefaultTextStyle(x =>
                                            x.FontColor(headerTextCol).FontSize(showPhoto ? 8f : 9f)
                                        );
                                        ComposeLinkRow(t, profile, false, headerTextCol);
                                    });

                                if (!string.IsNullOrWhiteSpace(profile.Tagline))
                                {
                                    col.Item()
                                        .PaddingTop(0.2f, Unit.Centimetre)
                                        .PaddingBottom(0.2f, Unit.Centimetre)
                                        .LineHorizontal(0.5f)
                                        .LineColor(titleTextCol);
                                    col.Item()
                                        .AlignCenter()
                                        .Text(t =>
                                        {
                                            t.AlignCenter();
                                            t.DefaultTextStyle(x =>
                                                x.FontColor(headerTextCol)
                                                    .FontSize(showPhoto ? 8.5f : 9.5f)
                                                    .LineHeight(1.2f)
                                            );
                                            ComposeMarkdownText(t, profile.Tagline, headerTextCol);
                                        });
                                }
                            });

                        if (showPhoto)
                        {
                            // Right Column (Spacer to balance the photo on the left)
                            row.ConstantItem(sideWidth, Unit.Centimetre).Element(_ => { });
                        }
                    });
            }
        });
    }

    private void ComposeContactRow(
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
            // We strip tags ONLY for the mailto: attribute to avoid broken links
            var cleanValue = HtmlTagRegex().Replace(rawValue, "").Trim();
            if (cleanValue.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                cleanValue = cleanValue[7..];

            // Use the raw value for display to support <strong> etc.
            var displayValue = isUpper ? rawValue.ToUpper() : rawValue;

            if (label == "EmailLabel")
            {
                // Wrap in mailto: link using the clean email address
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

    private void ComposeLinkRow(
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
            // Strip tags ONLY for the URL path
            var pureUrl = HtmlTagRegex().Replace(rawValue, "").Trim();
            var displayValue = isUpper ? rawValue.ToUpper() : rawValue;

            ComposeMarkdownText(t, $"<a href='{pureUrl}'>{displayValue}</a>", headerTextCol);
            first = false;
        }

        AddLink("LinkedInLabel", profile.LinkedInUrl);
        AddLink("GitHubLabel", profile.PortfolioUrl);
    }

    // --- Helper Methods for Page Sections ---

    private void ComposePageOne(
        IContainer container,
        CandidateProfile profile,
        float fontSize,
        CvTemplate template
    )
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(1, Unit.Centimetre);

            // Summary
            if (!string.IsNullOrWhiteSpace(profile.ProfessionalSummary))
            {
                col.Item()
                    .Background(_backgroundLight)
                    .BorderLeft(1.5f)
                    .BorderColor(_primaryColor)
                    .CornerRadius(5)
                    .Padding(10)
                    .Column(c =>
                    {
                        // Summary Text with HTML formatting support
                        ComposeHtmlContent(c, profile.ProfessionalSummary, fontSize, _textMedium);
                    });
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }

            // Skills (Core Competencies)
            if (profile.Skills != null && profile.Skills.Count != 0)
            {
                SectionTitle(col, _localizer["CoreCompetencies"], template);

                var categories = profile.Skills.GroupBy(s => s.Category ?? "Other").ToList();
                foreach (var cat in categories)
                {
                    col.Item()
                        .PaddingBottom(0.3f, Unit.Centimetre)
                        .Background(_backgroundLight)
                        .BorderLeft(1.5f)
                        .BorderColor(template == CvTemplate.Modern ? _accentColor : _primaryColor)
                        .CornerRadius(5)
                        .Padding(10)
                        .Column(c =>
                        {
                            c.Item()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(x =>
                                        x.Bold().FontSize(fontSize).FontColor(_primaryDark)
                                    );
                                    ComposeMarkdownText(t, cat.Key);
                                });
                            c.Item()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(x =>
                                        x.FontSize(fontSize - 1).FontColor(_textMedium)
                                    );
                                    var skillList = string.Join(
                                        ", ",
                                        cat.Select(s => s.Name).Distinct()
                                    );
                                    ComposeMarkdownText(t, skillList);
                                });
                        });
                }
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }
        });
    }

    private void ComposePageTwo(
        IContainer container,
        CandidateProfile profile,
        float fontSize,
        CvTemplate template
    )
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(1, Unit.Centimetre);

            // Experience
            if (profile.WorkExperience != null && profile.WorkExperience.Count != 0)
            {
                SectionTitle(col, _localizer["WorkExperienceCv"], template);

                col.Item()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(180);
                        });

                        var workExperiences = profile
                            .WorkExperience.OrderByDescending(e => e.StartDate)
                            .ToList();
                        for (int i = 0; i < workExperiences.Count; i++)
                        {
                            var exp = workExperiences[i];
                            var isLast = (i == workExperiences.Count - 1);

                            // Row 1: Title & Date
                            table
                                .Cell()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(s =>
                                        s.FontSize(fontSize + 1).FontColor(_textDark).Bold()
                                    );
                                    FormatHtmlToText(t, PreprocessHtml(exp.JobTitle));
                                });
                            table
                                .Cell()
                                .AlignRight()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(s =>
                                        s.FontSize(fontSize - 2).FontColor(_textMedium)
                                    );
                                    t.Span(
                                        $"{exp.StartDate:MM/yyyy} – {(exp.IsCurrentRole ? _localizer["Present"] : (exp.EndDate.HasValue ? exp.EndDate.Value.ToString("MM/yyyy") : _localizer["Present"]))}"
                                    );
                                    var duration = CalculateDuration(
                                        exp.StartDate,
                                        exp.EndDate,
                                        exp.IsCurrentRole
                                    );
                                    if (!string.IsNullOrEmpty(duration))
                                    {
                                        t.Span($" ({duration})").Italic();
                                    }
                                });

                            // Row 2: Company
                            table
                                .Cell()
                                .ColumnSpan(2)
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(x =>
                                        x.FontSize(fontSize)
                                            .FontColor(
                                                template == CvTemplate.Modern
                                                    ? _accentColor
                                                    : _primaryColor
                                            )
                                            .SemiBold()
                                    );
                                    var companyText = exp.CompanyName ?? "";
                                    if (!string.IsNullOrEmpty(exp.Location))
                                        companyText += $" - {exp.Location}";
                                    ComposeMarkdownText(t, companyText);
                                });

                            // Row 3: Description with Bullets
                            if (!string.IsNullOrWhiteSpace(exp.Description))
                            {
                                table
                                    .Cell()
                                    .ColumnSpan(2)
                                    .PaddingTop(0.2f, Unit.Centimetre)
                                    .Column(c =>
                                        ComposeHtmlContent(
                                            c,
                                            exp.Description,
                                            fontSize - 1,
                                            _textMedium,
                                            "",
                                            1.2f,
                                            0f
                                        )
                                    );
                            }

                            // Grey Divider Line
                            if (!isLast)
                            {
                                table
                                    .Cell()
                                    .ColumnSpan(2)
                                    .PaddingTop(0.4f, Unit.Centimetre)
                                    .PaddingBottom(0.4f, Unit.Centimetre)
                                    .LineHorizontal(1)
                                    .LineColor(_borderColor);
                            }
                        }
                    });
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }
        });
    }

    private void ComposePageThree(
        IContainer container,
        CandidateProfile profile,
        float fontSize,
        CvTemplate template
    )
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(1, Unit.Centimetre);

            // Education
            if (profile.Educations != null && profile.Educations.Count != 0)
            {
                SectionTitle(col, _localizer["EducationCv"], template);

                col.Item()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns => columns.RelativeColumn());

                        int count = profile.Educations.Count;
                        var eduList = profile
                            .Educations.OrderByDescending(e => e.StartDate)
                            .ToList();
                        for (int i = 0; i < count; i++)
                        {
                            var edu = eduList[i];
                            table
                                .Cell()
                                .Element(cell =>
                                {
                                    cell.Background(_backgroundLight)
                                        .BorderLeft(1.5f)
                                        .BorderColor(
                                            template == CvTemplate.Modern
                                                ? _accentColor
                                                : _accentColor
                                        ) // Modern uses accent for borders
                                        .CornerRadius(5)
                                        .Padding(10)
                                        .Column(c =>
                                        {
                                            c.Item()
                                                .Row(r =>
                                                {
                                                    r.RelativeItem()
                                                        .Text(t =>
                                                        {
                                                            t.DefaultTextStyle(x =>
                                                                x.Bold()
                                                                    .FontSize(fontSize + 1)
                                                                    .FontColor(_textDark)
                                                            );
                                                            ComposeMarkdownText(
                                                                t,
                                                                edu.Degree ?? ""
                                                            );
                                                        });
                                                    r.ConstantItem(100)
                                                        .AlignRight()
                                                        .Text(
                                                            $"{edu.StartDate:yyyy} - {(edu.EndDate.HasValue ? edu.EndDate.Value.ToString("yyyy") : _localizer["Present"])}"
                                                        )
                                                        .FontSize(fontSize - 2)
                                                        .FontColor(_textMedium);
                                                });

                                            c.Item()
                                                .Text(t =>
                                                {
                                                    t.DefaultTextStyle(x =>
                                                        x.FontSize(fontSize)
                                                            .FontColor(_primaryColor)
                                                            .SemiBold()
                                                            .Bold()
                                                    );
                                                    ComposeMarkdownText(
                                                        t,
                                                        edu.InstitutionName ?? ""
                                                    );
                                                });

                                            if (!string.IsNullOrEmpty(edu.Description))
                                            {
                                                // Separator line (matches CSS .recognition border-top)
                                                c.Item()
                                                    .PaddingTop(0.25f, Unit.Centimetre)
                                                    .PaddingBottom(0.25f, Unit.Centimetre)
                                                    .LineHorizontal(1)
                                                    .LineColor(_borderColor);

                                                // Handle HTML tags like <strong style=color:blue;font-weight:normal;>
                                                c.Item()
                                                    .Column(col =>
                                                        ComposeHtmlContent(
                                                            col,
                                                            edu.Description,
                                                            fontSize - 1,
                                                            _textMedium,
                                                            null,
                                                            1.2f,
                                                            0f
                                                        )
                                                    );
                                            }
                                        });
                                });

                            // Divider between Education Items (if not last)
                            if (i < count - 1)
                            {
                                table.Cell().ColumnSpan(1).LineHorizontal(1).LineColor("#E0E0E0");
                            }
                        }
                    });
                col.Item().PaddingBottom(0.3f, Unit.Centimetre);
            }

            // Divider
            col.Item()
                .PaddingTop(0.4f, Unit.Centimetre)
                .PaddingBottom(0.4f, Unit.Centimetre)
                .LineHorizontal(1)
                .LineColor(_borderColor);

            // Projects
            if (profile.Projects != null && profile.Projects.Count != 0)
            {
                SectionTitle(col, _localizer["PersonalProjectsCv"], template);

                col.Item()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns => columns.RelativeColumn());

                        foreach (var proj in profile.Projects.OrderByDescending(p => p.StartDate))
                        {
                            table
                                .Cell()
                                .PaddingBottom(0.5f, Unit.Centimetre)
                                .Element(cell =>
                                {
                                    cell.Background(_backgroundLight)
                                        .BorderLeft(1.5f)
                                        .BorderColor(_primaryColor)
                                        .CornerRadius(5)
                                        .Padding(10)
                                        .Column(c =>
                                        {
                                            c.Item()
                                                .Row(r =>
                                                {
                                                    r.RelativeItem()
                                                        .Text(t =>
                                                        {
                                                            t.DefaultTextStyle(x =>
                                                                x.Bold()
                                                                    .FontSize(fontSize + 1)
                                                                    .FontColor(_textDark)
                                                            );
                                                            ComposeMarkdownText(t, proj.Name ?? "");
                                                        });

                                                    var dateStr = "";
                                                    if (proj.StartDate.HasValue)
                                                    {
                                                        dateStr =
                                                            $"{proj.StartDate.Value:yyyy} - {(proj.EndDate.HasValue ? proj.EndDate.Value.ToString("yyyy") : _localizer["Present"])}";
                                                    }
                                                    r.ConstantItem(120)
                                                        .AlignRight()
                                                        .Text(dateStr)
                                                        .FontSize(fontSize - 2)
                                                        .FontColor(_textMedium);
                                                });

                                            if (
                                                !string.IsNullOrEmpty(proj.Link)
                                                || !string.IsNullOrEmpty(proj.Technologies)
                                            )
                                            {
                                                c.Item()
                                                    .PaddingBottom(0.1f, Unit.Centimetre)
                                                    .Text(t =>
                                                    {
                                                        t.DefaultTextStyle(x =>
                                                            x.FontSize(fontSize - 1)
                                                                .FontColor(_textMedium)
                                                        );
                                                        if (!string.IsNullOrEmpty(proj.Link))
                                                        {
                                                            t.Span($"{_localizer["GitHubLabel"]} ")
                                                                .Bold()
                                                                .FontColor(_primaryDark);
                                                            t.Span(proj.Link)
                                                                .FontColor(_primaryColor);
                                                            t.Span(" | ");
                                                        }
                                                        if (
                                                            !string.IsNullOrEmpty(proj.Technologies)
                                                        )
                                                        {
                                                            t.Span(
                                                                    $"{_localizer["TechnologiesLabel"]} "
                                                                )
                                                                .Bold()
                                                                .FontColor(_primaryDark);
                                                            ComposeMarkdownText(
                                                                t,
                                                                proj.Technologies
                                                            );
                                                        }
                                                    });
                                            }

                                            if (!string.IsNullOrEmpty(proj.Role))
                                            {
                                                c.Item()
                                                    .Text(t =>
                                                    {
                                                        t.DefaultTextStyle(x =>
                                                            x.FontSize(fontSize)
                                                                .FontColor(_primaryColor)
                                                                .SemiBold()
                                                        );
                                                        ComposeMarkdownText(t, proj.Role);
                                                    });
                                            }

                                            if (!string.IsNullOrEmpty(proj.Description))
                                            {
                                                // CSS uses ✓ (U+2713) for project features
                                                c.Item()
                                                    .PaddingTop(0.2f, Unit.Centimetre)
                                                    .Column(col =>
                                                        ComposeHtmlContent(
                                                            col,
                                                            proj.Description,
                                                            fontSize - 1,
                                                            _textMedium,
                                                            null,
                                                            1.2f,
                                                            0f
                                                        )
                                                    );
                                            }

                                            // Render SectionTitle if present (AI Analytics, etc.)
                                            // Render Section Description (Title + Description)
                                            if (!string.IsNullOrEmpty(proj.SectionDescription))
                                            {
                                                // New logic: Title is just title, Description is content
                                                if (!string.IsNullOrEmpty(proj.SectionTitle))
                                                {
                                                    c.Item()
                                                        .PaddingTop(0.2f, Unit.Centimetre)
                                                        .Text(t =>
                                                        {
                                                            t.DefaultTextStyle(x =>
                                                                x.SemiBold()
                                                                    .FontSize(fontSize)
                                                                    .FontColor(_primaryColor)
                                                            );
                                                            ComposeMarkdownText(
                                                                t,
                                                                proj.SectionTitle
                                                            );
                                                        });
                                                }

                                                c.Item()
                                                    .PaddingTop(0.1f, Unit.Centimetre)
                                                    .Column(col =>
                                                        ComposeHtmlContent(
                                                            col,
                                                            proj.SectionDescription,
                                                            fontSize - 1,
                                                            _textMedium,
                                                            null,
                                                            1.2f,
                                                            0f
                                                        )
                                                    );
                                            }
                                            else if (!string.IsNullOrEmpty(proj.SectionTitle))
                                            {
                                                var sectionLines = proj.SectionTitle.Split(
                                                    '\n',
                                                    StringSplitOptions.RemoveEmptyEntries
                                                );
                                                var sectionHeader = sectionLines
                                                    .FirstOrDefault()
                                                    ?.Trim();
                                                var sectionDetails = sectionLines.Skip(1).ToArray();

                                                // Render subsection title (like project name style)
                                                if (!string.IsNullOrEmpty(sectionHeader))
                                                {
                                                    c.Item()
                                                        .PaddingTop(0.2f, Unit.Centimetre)
                                                        .Text(t =>
                                                        {
                                                            t.DefaultTextStyle(x =>
                                                                x.SemiBold()
                                                                    .FontSize(fontSize)
                                                                    .FontColor(_primaryColor)
                                                            );
                                                            ComposeMarkdownText(t, sectionHeader);
                                                        });
                                                }

                                                // Render subsection details with bullets
                                                if (sectionDetails.Length > 0)
                                                {
                                                    c.Item()
                                                        .PaddingTop(0.1f, Unit.Centimetre)
                                                        .Column(col =>
                                                            ComposeHtmlContent(
                                                                col,
                                                                string.Join("\n", sectionDetails),
                                                                fontSize - 1,
                                                                _textMedium,
                                                                null,
                                                                1.2f,
                                                                0f
                                                            )
                                                        );
                                                }
                                            }
                                        });
                                });
                        }
                    });

                // Divider between Projects and Languages
                col.Item()
                    .PaddingTop(0.2f, Unit.Centimetre)
                    .PaddingBottom(0.3f, Unit.Centimetre)
                    .LineHorizontal(1)
                    .LineColor(_borderColor);
            }

            // Languages
            if (profile.Languages != null && profile.Languages.Count != 0)
            {
                SectionTitle(col, _localizer["LanguagesCv"], template);
                // Languages Layout: Stacked & Full Width (Table)
                col.Item()
                    .Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(9).FontColor(_textMedium));
                        t.AlignCenter();

                        // Bold language names: render each separately
                        for (int i = 0; i < profile.Languages.Count; i++)
                        {
                            var lang = profile.Languages[i];
                            ComposeMarkdownText(t, lang.Name);
                            t.Span(" ");
                            ComposeMarkdownText(t, lang.Proficiency);
                            if (i < profile.Languages.Count - 1)
                            {
                                t.Span(" | ");
                            }
                        }
                    });
                // CSS Section Separator: 0.4cm visual padding above/below line
                col.Item()
                    .PaddingTop(0.4f, Unit.Centimetre)
                    .PaddingBottom(0.4f, Unit.Centimetre)
                    .LineHorizontal(1)
                    .LineColor(_borderColor);
            }

            if (profile.Interests != null && profile.Interests.Count != 0)
            {
                SectionTitle(col, _localizer["InterestsCv"], template);

                // Tags Layout: Centered, allowed to wrap (2+ rows), rounded chips, restored font size
                col.Item()
                    .AlignCenter()
                    .Inlined(w =>
                    {
                        w.Spacing(4);

                        foreach (var interest in profile.Interests)
                        {
                            w.Item()
                                .Padding(2)
                                .Element(chip =>
                                {
                                    chip.Border(0.5f)
                                        .BorderColor(_borderColor)
                                        .Background(_backgroundLight)
                                        .CornerRadius(4)
                                        .PaddingHorizontal(2)
                                        .PaddingVertical(2)
                                        .Text(t =>
                                        {
                                            t.DefaultTextStyle(x =>
                                                x.FontSize(8).FontColor(_textMedium)
                                            );
                                            ComposeMarkdownText(t, interest.Name);
                                        });
                                });
                        }
                    });

                // Divider between Interests and Footer
                col.Item()
                    .PaddingTop(0.4f, Unit.Centimetre)
                    .PaddingBottom(0, Unit.Centimetre)
                    .LineHorizontal(1)
                    .LineColor(_borderColor);
            }

            // Footer Reference (Background, no gap)
            col.Item()
                .Background(_backgroundLight)
                .PaddingVertical(1)
                .PaddingHorizontal(2)
                .AlignCenter()
                .Text(_localizer["ReferencesAvailableUponRequest"])
                .FontSize(8)
                .FontColor(_textMedium)
                .Italic();
        });
    }

    private void SectionTitle(ColumnDescriptor column, string title, CvTemplate template)
    {
        column
            .Item()
            .Element(e =>
                (template == CvTemplate.Professional || template == CvTemplate.Modern)
                    ? e.AlignLeft()
                    : e
            )
            .PaddingBottom(0.3f, Unit.Centimetre)
            .PaddingTop(0.3f, Unit.Centimetre)
            .Element(e =>
            {
                if (template == CvTemplate.Modern)
                {
                    return e.PaddingBottom(0.1f, Unit.Centimetre)
                        .BorderLeft(4f)
                        .BorderColor(_accentColor)
                        .PaddingLeft(10);
                }
                return e;
            })
            .Row(row =>
            {
                row.AutoItem()
                    .Element(e =>
                    {
                        if (template == CvTemplate.Professional)
                            return e.BorderBottom(1.5f).BorderColor(_primaryColor);
                        if (template == CvTemplate.Minimalist)
                        {
                            return e.Width(17, Unit.Centimetre)
                                .BorderBottom(1.5f)
                                .BorderColor(_primaryDark)
                                .PaddingBottom(2);
                        }

                        return e;
                    })
                    .Text(title.ToUpper())
                    .FontSize(template == CvTemplate.Minimalist ? 11 : 12)
                    .Bold()
                    .FontColor(template == CvTemplate.Modern ? _primaryColor : _primaryDark)
                    .LetterSpacing(template == CvTemplate.Minimalist ? 0.15f : 0.06f);
            });
    }

    private static void FormatHtmlToText(
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
            // Add text before this match
            if (match.Index > lastIndex)
            {
                var beforeText = input[lastIndex..match.Index];
                // Strip whitespace for non-block content
                var cleanBefore = HtmlTagRegex().Replace(beforeText, string.Empty);
                if (
                    !string.IsNullOrWhiteSpace(cleanBefore)
                    || (isBlock && !string.IsNullOrEmpty(cleanBefore))
                )
                {
                    var span = !string.IsNullOrEmpty(linkUrl)
                        ? textDescriptor.Hyperlink(linkUrl, cleanBefore)
                        : textDescriptor.Span(cleanBefore);

                    // Apply context color to link if no specific tag color exists
                    var finalColor =
                        color ?? (string.IsNullOrEmpty(linkUrl) ? null : fallbackColor);
                    ApplyStyle(span, isBold, isItalic, isUnderline, finalColor);
                }
            }

            var tagName = match.Groups[1].Value.ToLower();
            var attributes = match.Groups[2].Value;
            var content = match.Groups[3].Value;

            // Inherit parent styles or apply new ones from current tag
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

                    var fontStyleMatch = FontStyleStyleRegex().Match(styleAttr);
                    if (fontStyleMatch.Success)
                    {
                        var style = fontStyleMatch.Groups[1].Value.Trim().ToLower();
                        if (style == "italic")
                            currentItalic = true;
                        else if (style == "normal")
                            currentItalic = false;
                    }
                }
            }

            // RECURSIVELY handle nested tags
            FormatHtmlToText(
                textDescriptor,
                content,
                isBlock,
                fallbackColor ?? color, // Pass current color as fallback for children
                currentBold,
                currentItalic,
                currentUnderline,
                currentColor,
                currentLink
            );

            // Add a newline for block-level tags only if we are in a block context
            // AND there is more content (tags or text) following this tag.
            // AND the following content doesn't already start with a newline.
            if (
                isBlock
                && (
                    tagName == "p"
                    || tagName == "div"
                    || tagName.StartsWith('h')
                    || tagName == "blockquote"
                    || tagName == "pre"
                )
            )
            {
                var remaining = input[(match.Index + match.Length)..];
                if (
                    HtmlTagRegex().IsMatch(remaining)
                    || (!string.IsNullOrWhiteSpace(remaining) && remaining.Trim().Length > 0)
                )
                {
                    if (!remaining.TrimStart(' ', '\t', '\r').StartsWith('\n'))
                    {
                        textDescriptor.Span("\n");
                    }
                }
            }

            lastIndex = match.Index + match.Length;
        }

        // Add remaining text
        if (lastIndex < input.Length)
        {
            var remainingText = input[lastIndex..];
            // Check if remaining text has actual content or if we should skip whitespace in non-block mode
            var cleanRemaining = HtmlTagRegex().Replace(remainingText, string.Empty);
            if (
                !string.IsNullOrWhiteSpace(cleanRemaining)
                || (isBlock && !string.IsNullOrEmpty(cleanRemaining))
            )
            {
                var span = !string.IsNullOrEmpty(linkUrl)
                    ? textDescriptor.Hyperlink(linkUrl, cleanRemaining)
                    : textDescriptor.Span(cleanRemaining);

                // If this is a link and no specific color was found in the HTML, use the fallbackColor or theme color
                var finalColor = color ?? (string.IsNullOrEmpty(linkUrl) ? null : fallbackColor);
                ApplyStyle(span, isBold, isItalic, isUnderline, finalColor);
            }
        }
    }

    private static void ApplyStyle(
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

    // SVG Path for Checkmark (Material Design)
    private const string CheckmarkSvgPath = "M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z";

    private string PreprocessHtml(string? input, bool isBlock = true, string? bullet = null)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        string pText = WebUtility.HtmlDecode(input ?? "");

        // Strip mailto: from input text before processing to avoid display issues
        if (pText.Contains("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            pText = pText.Replace("mailto:", "", StringComparison.OrdinalIgnoreCase);
        }

        // Run Markdig for full markdown support
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        pText = Markdown.ToHtml(pText, pipeline);

        if (!isBlock)
        {
            pText = pText.Trim();
            // If it's a single paragraph, strip the <p> tags for inline rendering
            if (
                pText.StartsWith("<p>", StringComparison.OrdinalIgnoreCase)
                && pText.EndsWith("</p>", StringComparison.OrdinalIgnoreCase)
                && pText.IndexOf("<p>", 3, StringComparison.OrdinalIgnoreCase) == -1
            ) // Only one <p> tag
            {
                pText = pText[3..^4];
            }
        }

        // Handle HR tags - Convert to unique placeholder
        pText = HrTagRegex().Replace(pText, "[[HR]]");

        const string checkmarkPlaceholder = "[[CHECKMARK]]";

        // Handle Lists (<ul><li>...</li></ul>)
        if (pText.Contains("<li>", StringComparison.OrdinalIgnoreCase))
        {
            // 1. Strip nested <p> tags inside <li> to prevent extra gaps from FormatHtmlToText
            pText = LiWithNestedPRegex().Replace(pText, "<li>$1</li>");

            // 2. Determine bullet replacement
            string bRep = bullet ?? "";
            if (!string.IsNullOrEmpty(bullet))
            {
                if (bullet.Contains('\u25B8'))
                    bRep = $"<span style='color:{_primaryColor}'>\u25B8</span> ";
                else if (bullet.Contains('\u2713'))
                    bRep = checkmarkPlaceholder;
            }

            // 3. Flatten accurately: remove <ul>/</ul>, strip whitespace around </li>,
            // and replace <li> (with its surrounding whitespace) with \n + bullet.
            pText = ListTagRegex().Replace(pText, "");
            pText = LiCloseWithWhitespaceRegex().Replace(pText, "");
            pText = LiOpenWithWhitespaceRegex().Replace(pText, "\n" + bRep);

            pText = pText.Trim().Replace("&#8226;", "• ");

            // Support manual bullets and checkmarks typed in markdown
            // Convert manual checkmarks at start of lines to the styled SVG placeholder
            pText = pText.Replace("\n\u2713", "\n" + checkmarkPlaceholder);
            if (pText.StartsWith('\u2713'))
                pText = checkmarkPlaceholder + pText[1..];

            // Ensure manual dot bullets have a space
            pText = pText.Replace("\n•", "\n• ");
            if (pText.StartsWith('•'))
                pText = "• " + pText[1..];
        }
        else if (
            !string.IsNullOrEmpty(bullet)
            && !pText.Contains("<p>", StringComparison.OrdinalIgnoreCase)
        )
        {
            // Plain text with newlines - convert to bullets if requested
            var lines = pText.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();

            string bulletReplacement = bullet;
            if (bullet.Contains('\u25B8')) // ▸ Right Arrow
            {
                bulletReplacement = $"<span style='color:{_primaryColor}'>\u25B8</span> ";
            }
            else if (bullet.Contains('\u2713')) // ✓ Checkmark
            {
                bulletReplacement = checkmarkPlaceholder;
            }

            foreach (var line in lines)
            {
                var cleanLine = line.Trim();

                // Explicitly handle <br> tags here because we return early from this block
                // transforming them into newlines *within* the same bullet point
                cleanLine = BrTagRegex().Replace(cleanLine, "\n");

                if (!string.IsNullOrEmpty(cleanLine))
                    sb.AppendLine($"{bulletReplacement}{cleanLine}");
            }
            pText = sb.ToString().Trim();
        }

        pText = BrTagRegex().Replace(pText, "\n");

        // Advanced Cleanup:
        // 1. Collapse multiple consecutive newlines into a maximum of two (or one for high density)
        // For CVs, we typically want a single newline between blocks unless explicitly spaced.
        pText = MultipleNewlineRegex().Replace(pText, "\n");

        // 2. Remove whitespace between block tags that often causes "ghost" lines
        // e.g. "</p>  \n  <p>" -> "</p><p>"
        pText = BlockGapsRegex().Replace(pText, "$1$2");

        // Decode HTML entities
        // Entities already decoded at top

        // Safety net: Identify standalone occurrences of checkmark chars
        // Replace existing unicode checkmarks in the text with the placeholder
        pText = pText.Replace("\u2713", checkmarkPlaceholder);

        // Handle standalone right arrows (keep as text span)
        pText = pText.Replace("\u25B8", $"<span style='color:{_primaryColor}'>\u25B8</span>");

        // Auto-link URLs and emails (very basic detection, avoiding already linked ones)
        if (!pText.Contains("<a", StringComparison.OrdinalIgnoreCase))
        {
            pText = AutoLinkRegex()
                .Replace(
                    pText,
                    match =>
                    {
                        var val = match.Value;

                        // Check if it's an email - handle with mailto:
                        if (
                            val.Contains('@')
                            && !val.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        )
                        {
                            var email = val.StartsWith(
                                "mailto:",
                                StringComparison.OrdinalIgnoreCase
                            )
                                ? val[7..]
                                : val;
                            return $"<a href='mailto:{email}'>{email}</a>";
                        }

                        // Handle standard URLs
                        if (val.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
                        {
                            return $"<a href='http://{val}'>{val}</a>";
                        }

                        // For already specified http/https URLs
                        if (val.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            return $"<a href='{val}'>{val}</a>";
                        }

                        // Default case (should be caught by above, but as a safety)
                        return $"<a href='{val}'>{val}</a>";
                    }
                );
        }

        return pText.Trim();
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
            parts.Add($"{years} {_localizer[years > 1 ? "Years" : "Year"]}");
        if (months > 0)
            parts.Add($"{months} {_localizer[months > 1 ? "Months" : "Month"]}");

        return string.Join(" ", parts);
    }

    private static int GetPageCount(byte[] pdfBytes)
    {
        var text = Encoding.Default.GetString(pdfBytes);
        var matches = PageTypeRegex().Matches(text);
        return matches.Count;
    }

    // GeneratedRegex patterns
    [GeneratedRegex(@"<[a-zA-Z/][^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("^[0-9a-fA-F]{3,8}$")]
    private static partial Regex HexColorRegex();

    [GeneratedRegex(@"/Type\s*/Page\b", RegexOptions.IgnoreCase)]
    private static partial Regex PageTypeRegex();

#pragma warning disable SYSLIB1045 // Pattern with backreferences not supported by GeneratedRegex
    private static readonly Regex HtmlTagWithStyleRegexInstance = new(
        @"<(strong|b|em|i|u|span|div|p|h1|h2|h3|h4|h5|h6|a|blockquote|code|pre)(?:\s+([^>]*?))?\s*>(.+?)</\1>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline
    );
#pragma warning restore SYSLIB1045

    [GeneratedRegex(@"href\s*=\s*(?:""([^""]*)""|'([^']*)'|([^""'\s>]+))", RegexOptions.IgnoreCase)]
    private static partial Regex HrefAttributeRegex();

    private static Regex HtmlTagWithStyleRegex() => HtmlTagWithStyleRegexInstance;

    [GeneratedRegex(
        @"style\s*=\s*(?:""([^""]*)""|'([^']*)'|([^""'\s>]+))",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex StyleAttributeRegex();

    [GeneratedRegex(@"color\s*:\s*([^;""'\s]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ColorStyleRegex();

    [GeneratedRegex(@"font-weight\s*:\s*([^;]+)", RegexOptions.IgnoreCase)]
    private static partial Regex FontWeightStyleRegex();

    [GeneratedRegex(@"font-style\s*:\s*([^;]+)", RegexOptions.IgnoreCase)]
    private static partial Regex FontStyleStyleRegex();

    [GeneratedRegex(@"<ul>|</ul>|<ol>|</ol>", RegexOptions.IgnoreCase)]
    private static partial Regex ListTagRegex();

    [GeneratedRegex(@"<br.*?>", RegexOptions.IgnoreCase)]
    private static partial Regex BrTagRegex();

    [GeneratedRegex(@"<hr\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex HrTagRegex();

    [GeneratedRegex(
        @"\b((?:https?://|www\.)[^\s<]*[^\s<.,!?;:]|[a-zA-Z0-9._%+-]+@(?:[a-zA-Z0-9-]+\.)+[a-zA-Z]{2,})\b",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex AutoLinkRegex();

    [GeneratedRegex(@"</li>\s*", RegexOptions.IgnoreCase)]
    private static partial Regex LiCloseWithWhitespaceRegex();

    [GeneratedRegex(@"\s*<li>", RegexOptions.IgnoreCase)]
    private static partial Regex LiOpenWithWhitespaceRegex();

    [GeneratedRegex(
        @"<li>\s*<p>(.*?)</p>\s*</li>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline
    )]
    private static partial Regex LiWithNestedPRegex();

    [GeneratedRegex(@"\n{2,}")]
    private static partial Regex MultipleNewlineRegex();

    [GeneratedRegex(
        @"(</p>|</div>|</h1>|</h2>|h3>|</h4>|</h5>|</h6>)\s+(<p|<div>|<h1|<h2|<h3|<h4|<h5|<h6)",
        RegexOptions.IgnoreCase
    )]
    private static partial Regex BlockGapsRegex();

    /// <summary>
    /// Renders text with full markdown support into a TextDescriptor.
    /// Used for single-line or inline candidate-provided fields.
    /// </summary>
    private void ComposeMarkdownText(TextDescriptor t, string? content, string? color = null)
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

    /// <summary>
    /// Renders HTML content into a ColumnDescriptor, handling text blocks, horizontal lines, and SVG checkmarks.
    /// Replaces direct Text() calls to support block-level elements like HR and complex Layouts for Checkmarks.
    /// </summary>
    private void ComposeHtmlContent(
        ColumnDescriptor column,
        string? input,
        float fontSize,
        string fontColor,
        string? bullet = null,
        float lineHeight = 1.2f,
        float paragraphSpacing = 0f
    )
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        // Preprocess will convert bullets/checkmarks to placeholders if needed
        var preprocessed = PreprocessHtml(input, isBlock: true, bullet);
        var parts = preprocessed.Split(["[[HR]]"], StringSplitOptions.None);

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];

            if (!string.IsNullOrWhiteSpace(part))
            {
                // Split by Checkmark Placeholder
                var checkmarkParts = part.Split(["[[CHECKMARK]]"], StringSplitOptions.None);

                for (int j = 0; j < checkmarkParts.Length; j++)
                {
                    var segment = checkmarkParts[j];

                    // j=0: Text before the first checkmark (if any)
                    // j>0: Text following a checkmark (so render checkmark + text)

                    if (j == 0)
                    {
                        if (!string.IsNullOrWhiteSpace(segment))
                        {
                            column
                                .Item()
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
                                        segment,
                                        isBlock: true,
                                        fallbackColor: fontColor
                                    );
                                });
                        }
                    }
                    else
                    {
                        // Render Checkmark + Text segment
                        column
                            .Item()
                            .Row(row =>
                            {
                                // Fixed width for the checkmark icon
                                // Scaling icon slightly down to match text baseline better
                                var svgXml =
                                    $"<svg viewBox=\"0 0 24 24\"><path d=\"{CheckmarkSvgPath}\" fill=\"{_accentColor}\"/></svg>";
                                row.ConstantItem(fontSize + 5)
                                    .PaddingRight(5)
                                    .PaddingTop(2)
                                    .Height(fontSize)
                                    .Svg(svgXml);

                                // Remaining width for the text
                                row.RelativeItem()
                                    .Text(t =>
                                    {
                                        t.DefaultTextStyle(s =>
                                            s.FontSize(fontSize)
                                                .FontColor(fontColor)
                                                .LineHeight(lineHeight)
                                        );
                                        t.ParagraphSpacing(paragraphSpacing);
                                        // Even if segment is empty (e.g. checkmark only), FormatHtmlToText is safe
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

            // Render HR if not the last part
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
}
