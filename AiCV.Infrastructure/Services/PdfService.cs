namespace AiCV.Infrastructure.Services;

public partial class PdfService(IWebHostEnvironment env, IStringLocalizer<AicvResources> localizer)
    : IPdfService
{
    private readonly IWebHostEnvironment _env = env;
    private readonly IStringLocalizer<AicvResources> _localizer = localizer;

    // Define colors
    // Define colors from CSS
    private const string PrimaryColor = "#2c7be5"; // var(--primary-color)
    private const string PrimaryDark = "#1e5fae"; // var(--primary-dark)
    private const string AccentColor = "#10b981"; // var(--accent-color)
    private const string TextDark = "#1f2937"; // var(--text-dark)
    private const string TextMedium = "#4b5563"; // var(--text-medium)
    private const string BackgroundLight = "#f9fafb"; // var(--bg-light)
    private const string BorderColor = "#e5e7eb"; // var(--border-color)

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
        colorName = colorName.Trim();
        if (!colorName.StartsWith('#'))
        {
            if (NamedColors.TryGetValue(colorName, out var hex))
                return hex;
            return null;
        }

        return colorName;
    }

    public Task<byte[]> GenerateCvAsync(CandidateProfile profile)
    {
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
                    page.Margin(1.25f, Unit.Centimetre);
                    page.DefaultTextStyle(x =>
                        x.FontSize(size).FontFamily("Lato").FontColor(TextDark)
                    );
                    page.Header().ShowOnce().Element(c => ComposeHeader(c, profile));
                    page.Content().Element(c => ComposePageOne(c, profile, size));
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
                    page.Margin(1.25f, Unit.Centimetre);
                    page.DefaultTextStyle(x =>
                        x.FontSize(size).FontFamily("Lato").FontColor(TextDark)
                    );
                    page.Content().Element(c => ComposePageTwo(c, profile, size));
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
                page.Margin(1.25f, Unit.Centimetre);
                page.DefaultTextStyle(x =>
                    x.FontSize(page1Size).FontFamily("Lato").FontColor(TextDark)
                );
                page.Header().Element(c => ComposeHeader(c, profile));
                page.Content().Element(c => ComposePageOne(c, profile, page1Size));
            });

            // Page 2
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.25f, Unit.Centimetre);
                page.DefaultTextStyle(x =>
                    x.FontSize(page2Size).FontFamily("Lato").FontColor(TextDark)
                );
                page.Content().Element(c => ComposePageTwo(c, profile, page2Size));
            });

            // Page 3+
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.25f, Unit.Centimetre);
                page.DefaultTextStyle(x =>
                    x.FontSize(page3Size).FontFamily("Lato").FontColor(TextDark)
                );
                page.Content().Element(c => ComposePageThree(c, profile, page3Size));
            });
        });

        byte[] pdfBytes = document.GeneratePdf();
        return Task.FromResult(pdfBytes);
    }

    public Task<byte[]> GenerateCoverLetterAsync(
        string letterContent,
        CandidateProfile profile,
        string jobTitle,
        string companyName
    )
    {
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
                    page.Margin(1.25f, Unit.Centimetre);
                    page.DefaultTextStyle(x =>
                        x.FontSize(size).FontFamily("Lato").FontColor(TextDark)
                    );

                    page.Header().ShowOnce().Element(c => ComposeHeader(c, profile));

                    page.Content()
                        .Column(col =>
                        {
                            // Body with left border and background (matching .summary style)
                            col.Item().PaddingTop(0.8f, Unit.Centimetre);

                            // Wrap the entire letter content in a styled container (thinner: 1.5pt)
                            col.Item()
                                .Background(BackgroundLight)
                                .BorderLeft(1.5f)
                                .BorderColor(PrimaryColor)
                                .CornerRadius(5)
                                .Padding(10)
                                .Column(letterCol =>
                                {
                                    // // Date
                                    // letterCol
                                    //     .Item()
                                    //     .Text(DateTime.Now.ToString("MMMM dd, yyyy"))
                                    //     .FontSize(size);
                                    // letterCol.Item().PaddingBottom(0.8f, Unit.Centimetre);

                                    // Subject removed (included in generated content)

                                    // Main content
                                    if (!string.IsNullOrWhiteSpace(letterContent))
                                    {
                                        foreach (
                                            var paragraph in letterContent.Split(
                                                ["\n", "\r\n"],
                                                StringSplitOptions.RemoveEmptyEntries
                                            )
                                        )
                                        {
                                            letterCol
                                                .Item()
                                                .Text(paragraph.Trim())
                                                .FontSize(size)
                                                .LineHeight(1.5f);
                                            letterCol.Item().PaddingBottom(0.35f, Unit.Centimetre);
                                        }
                                    }
                                    else
                                    {
                                        letterCol.Item().Text("No content provided.").Italic();
                                    }

                                    // Sign-off removed (included in generated content)
                                });
                        });

                    // No footer for cover letter
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

    private void ComposeHeader(IContainer container, CandidateProfile profile)
    {
        // Header from CSS: Background Gradient (Simulated with Primary), Color White, Left Aligned with Image
        // Determine if photo will be shown to calculate padding
        bool showPhoto =
            profile.ShowProfilePicture && !string.IsNullOrEmpty(profile.ProfilePictureUrl);
        float textPaddingLeft = showPhoto ? 4f : 1f; // 4cm when photo, 1cm otherwise (matches cv-print.css)

        container.Column(c =>
        {
            c.Item()
                .Background(PrimaryColor)
                .Layers(layers =>
                {
                    // 1. Text Layer (Primary - Centered in remaining space)
                    layers
                        .PrimaryLayer()
                        .PaddingVertical(1, Unit.Centimetre)
                        .PaddingLeft(textPaddingLeft, Unit.Centimetre)
                        .PaddingRight(1, Unit.Centimetre)
                        .MinHeight(3f, Unit.Centimetre) // Ensure height for image
                        .Column(col =>
                        {
                            // Name
                            col.Item()
                                .AlignCenter()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(x =>
                                        x.FontSize(20).Bold().FontColor("#ffffff")
                                    );
                                    ComposeMarkdownText(t, profile.FullName);
                                });

                            // Title
                            col.Item()
                                .AlignCenter()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(x =>
                                        x.FontSize(11).FontColor(Colors.Grey.Lighten4)
                                    );
                                    FormatHtmlToText(t, PreprocessHtml(profile.Title ?? ""));
                                });

                            // Contact Info
                            col.Item()
                                .PaddingTop(0.2f, Unit.Centimetre)
                                .AlignCenter()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(x => x.FontColor("#ffffff").FontSize(9));
                                    t.DefaultTextStyle(x => x.FontColor("#ffffff").FontSize(9));

                                    bool first = true;
                                    void AddPart(string label, string? value)
                                    {
                                        if (string.IsNullOrEmpty(value))
                                            return;
                                        if (!first)
                                            t.Span(" | ");
                                        t.Span($"{_localizer[label]} ");
                                        FormatHtmlToText(t, PreprocessHtml(value));
                                        first = false;
                                    }

                                    AddPart("EmailLabel", profile.Email);
                                    AddPart("PhoneLabel", profile.PhoneNumber);
                                    AddPart("LocationLabel", profile.Location);
                                });

                            // Links
                            col.Item()
                                .PaddingTop(0.1f, Unit.Centimetre)
                                .AlignCenter()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(x => x.FontColor("#ffffff").FontSize(9));

                                    bool first = true;
                                    void AddLink(string label, string? value)
                                    {
                                        if (string.IsNullOrEmpty(value))
                                            return;
                                        if (!first)
                                            t.Span(" | ");
                                        t.Span($"{_localizer[label]} ");
                                        FormatHtmlToText(t, PreprocessHtml(value));
                                        first = false;
                                    }

                                    AddLink("LinkedInLabel", profile.LinkedInUrl);
                                    AddLink("GitHubLabel", profile.PortfolioUrl);
                                });

                            // Tagline
                            if (!string.IsNullOrEmpty(profile.Tagline))
                            {
                                col.Item()
                                    .PaddingTop(0.05f, Unit.Centimetre)
                                    .PaddingBottom(0.05f, Unit.Centimetre)
                                    .LineHorizontal(0.1f)
                                    .LineColor("#ffffff");

                                col.Item()
                                    .AlignCenter()
                                    .Text(t =>
                                    {
                                        t.DefaultTextStyle(x =>
                                            x.FontSize(9).FontColor("#ffffff").Medium()
                                        );
                                        ComposeMarkdownText(t, profile.Tagline);
                                    });
                            }
                        });

                    // 2. Image Layer (Overlay - Left Aligned)
                    if (showPhoto)
                    {
                        var path = Path.Combine(
                            _env.WebRootPath,
                            profile.ProfilePictureUrl!.TrimStart('/', '\\')
                        );
                        if (File.Exists(path))
                        {
                            layers
                                .Layer()
                                .AlignMiddle()
                                .AlignLeft()
                                .PaddingLeft(1, Unit.Centimetre) // ~2cm left margin
                                .Width(2.5f, Unit.Centimetre) // 6.25em ~ 2.5cm (print size)
                                .Height(2.5f, Unit.Centimetre)
                                .Element(e =>
                                {
                                    e.Background("#ffffff") // Fill gaps with white
                                        .CornerRadius(1.25f, Unit.Centimetre)
                                        .Border(2)
                                        .BorderColor("#ffffff")
                                        .Image(path)
                                        .FitArea();
                                });
                        }
                    }
                });

            // Bottom Accent Line - Full Width (thinner: 0.04cm)
            c.Item().Height(0.04f, Unit.Centimetre).Background(AccentColor);
        });
    }

    // --- Helper Methods for Page Sections ---

    private void ComposePageOne(IContainer container, CandidateProfile profile, float fontSize)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(1, Unit.Centimetre);

            // Summary
            if (!string.IsNullOrWhiteSpace(profile.ProfessionalSummary))
            {
                col.Item()
                    .Background(BackgroundLight)
                    .BorderLeft(1.5f)
                    .BorderColor(PrimaryColor)
                    .CornerRadius(5)
                    .Padding(10)
                    .Column(c =>
                    {
                        // Summary Text with HTML formatting support
                        ComposeHtmlContent(c, profile.ProfessionalSummary, fontSize, TextMedium);
                    });
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }

            // Skills (Core Competencies)
            if (profile.Skills != null && profile.Skills.Count != 0)
            {
                SectionTitle(col, _localizer["CoreCompetencies"]);

                var categories = profile.Skills.GroupBy(s => s.Category ?? "Other").ToList();
                foreach (var cat in categories)
                {
                    col.Item()
                        .PaddingBottom(0.3f, Unit.Centimetre)
                        .Background(BackgroundLight)
                        .BorderLeft(1.5f)
                        .BorderColor(PrimaryColor)
                        .CornerRadius(5)
                        .Padding(10)
                        .Column(c =>
                        {
                            c.Item()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(x =>
                                        x.Bold().FontSize(fontSize).FontColor(PrimaryDark)
                                    );
                                    ComposeMarkdownText(t, cat.Key);
                                });
                            c.Item()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(x =>
                                        x.FontSize(fontSize - 1).FontColor(TextMedium)
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

    private void ComposePageTwo(IContainer container, CandidateProfile profile, float fontSize)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(1, Unit.Centimetre);

            // Experience
            if (profile.WorkExperience != null && profile.WorkExperience.Count != 0)
            {
                SectionTitle(col, _localizer["WorkExperienceCv"]);

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
                                        s.FontSize(fontSize + 1).FontColor(TextDark).Bold()
                                    );
                                    FormatHtmlToText(t, PreprocessHtml(exp.JobTitle));
                                });
                            table
                                .Cell()
                                .AlignRight()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(s =>
                                        s.FontSize(fontSize - 2).FontColor(TextMedium)
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
                                        x.FontSize(fontSize).FontColor(PrimaryColor).SemiBold()
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
                                            TextMedium,
                                            "• "
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
                                    .LineColor(BorderColor);
                            }
                        }
                    });
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }
        });
    }

    private void ComposePageThree(IContainer container, CandidateProfile profile, float fontSize)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(1, Unit.Centimetre);

            // Education
            if (profile.Educations != null && profile.Educations.Count != 0)
            {
                SectionTitle(col, _localizer["EducationCv"]);

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
                                    cell.Background(BackgroundLight)
                                        .BorderLeft(1.5f)
                                        .BorderColor(AccentColor)
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
                                                                    .FontColor(TextDark)
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
                                                        .FontColor(TextMedium);
                                                });

                                            c.Item()
                                                .Text(t =>
                                                {
                                                    t.DefaultTextStyle(x =>
                                                        x.FontSize(fontSize)
                                                            .FontColor(PrimaryColor)
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
                                                    .LineHorizontal(1)
                                                    .LineColor(BorderColor);

                                                // Handle HTML tags like <strong style=color:blue;font-weight:normal;>
                                                c.Item()
                                                    .Column(col =>
                                                        ComposeHtmlContent(
                                                            col,
                                                            edu.Description,
                                                            fontSize - 1,
                                                            TextMedium
                                                        )
                                                    );
                                            }
                                        });
                                });

                            // Divider between Education Items (if not last)
                            if (i < count - 1)
                            {
                                table
                                    .Cell()
                                    .ColumnSpan(1)
                                    .LineHorizontal(1)
                                    .LineColor(Colors.Grey.Lighten2);
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
                .LineColor(BorderColor);

            // Projects
            if (profile.Projects != null && profile.Projects.Count != 0)
            {
                SectionTitle(col, _localizer["PersonalProjectsCv"]);

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
                                    cell.Background(BackgroundLight)
                                        .BorderLeft(1.5f)
                                        .BorderColor(PrimaryColor)
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
                                                                    .FontColor(TextDark)
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
                                                        .FontColor(TextMedium);
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
                                                                .FontColor(TextMedium)
                                                        );
                                                        if (!string.IsNullOrEmpty(proj.Link))
                                                        {
                                                            t.Span($"{_localizer["GitHubLabel"]} ")
                                                                .Bold()
                                                                .FontColor(PrimaryDark);
                                                            t.Span(proj.Link)
                                                                .FontColor(PrimaryColor);
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
                                                                .FontColor(PrimaryDark);
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
                                                                .FontColor(PrimaryColor)
                                                                .SemiBold()
                                                        );
                                                        ComposeMarkdownText(t, proj.Role);
                                                    });
                                            }

                                            if (!string.IsNullOrEmpty(proj.Description))
                                            {
                                                // CSS uses ✓ (U+2713) for project features
                                                c.Item()
                                                    .Column(col =>
                                                        ComposeHtmlContent(
                                                            col,
                                                            proj.Description,
                                                            fontSize - 1,
                                                            TextMedium,
                                                            "\u2713 "
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
                                                                    .FontColor(PrimaryColor)
                                                            );
                                                            ComposeMarkdownText(
                                                                t,
                                                                proj.SectionTitle
                                                            );
                                                        });
                                                }

                                                c.Item()
                                                    .Column(col =>
                                                        ComposeHtmlContent(
                                                            col,
                                                            proj.SectionDescription,
                                                            fontSize - 1,
                                                            TextMedium,
                                                            "\u2713 "
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
                                                                    .FontColor(PrimaryColor)
                                                            );
                                                            ComposeMarkdownText(t, sectionHeader);
                                                        });
                                                }

                                                // Render subsection details with bullets
                                                if (sectionDetails.Length > 0)
                                                {
                                                    c.Item()
                                                        .Column(col =>
                                                            ComposeHtmlContent(
                                                                col,
                                                                string.Join("\n", sectionDetails),
                                                                fontSize - 1,
                                                                TextMedium,
                                                                "\u2713 "
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
                    .LineColor(BorderColor);
            }

            // Languages
            if (profile.Languages != null && profile.Languages.Count != 0)
            {
                SectionTitle(col, _localizer["LanguagesCv"]);
                // Languages Layout: Stacked & Full Width (Table)
                col.Item()
                    .Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(9).FontColor(TextMedium));
                        t.AlignCenter();
                        var langTexts = new List<string>();
                        foreach (var lang in profile.Languages)
                        {
                            langTexts.Add(
                                $"{StripHtml(lang.Name)} ({StripHtml(lang.Proficiency)})"
                            );
                        }
                        // Bold language names: render each separately
                        for (int i = 0; i < profile.Languages.Count; i++)
                        {
                            var lang = profile.Languages[i];
                            ComposeMarkdownText(t, lang.Name);
                            t.Span(" (");
                            ComposeMarkdownText(t, lang.Proficiency);
                            t.Span(")").FontSize(8).Italic();
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
                    .LineColor(BorderColor);
            }

            if (profile.Interests != null && profile.Interests.Count != 0)
            {
                SectionTitle(col, _localizer["InterestsCv"]);

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
                                        .BorderColor(BorderColor)
                                        .Background(BackgroundLight)
                                        .CornerRadius(4)
                                        .PaddingHorizontal(2)
                                        .PaddingVertical(2)
                                        .Text(t =>
                                        {
                                            t.DefaultTextStyle(x =>
                                                x.FontSize(8).FontColor(TextMedium)
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
                    .LineColor(BorderColor);
            }

            // Footer Reference (Background, no gap)
            col.Item()
                .Background(BackgroundLight)
                .PaddingVertical(1)
                .PaddingHorizontal(2)
                .AlignCenter()
                .Text(_localizer["ReferencesAvailableUponRequest"])
                .FontSize(8)
                .FontColor(TextMedium)
                .Italic();
        });
    }

    private static void SectionTitle(ColumnDescriptor column, string title)
    {
        column
            .Item()
            .PaddingBottom(0.3f, Unit.Centimetre)
            .Row(row =>
            {
                // CSS: display: inline-block; border-bottom: ... (Matches content width)
                row.AutoItem()
                    .BorderBottom(1.5f)
                    .BorderColor(PrimaryColor)
                    .Text(title.ToUpper())
                    .FontSize(12)
                    .Bold()
                    .FontColor(PrimaryDark)
                    .LetterSpacing(0.06f);
            });
    }

    private static void FormatHtmlToText(
        TextDescriptor textDescriptor,
        string? input,
        bool isBold = false,
        bool isItalic = false,
        bool isUnderline = false,
        string? color = null
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
                var cleanBefore = HtmlTagRegex().Replace(beforeText, string.Empty);
                if (!string.IsNullOrEmpty(cleanBefore))
                {
                    var span = textDescriptor.Span(cleanBefore);
                    ApplyStyle(span, isBold, isItalic, isUnderline, color);
                }
            }

            var tagName = match.Groups[1].Value.ToLower();
            var styleAttr = match.Groups[2].Value + match.Groups[3].Value + match.Groups[4].Value;
            var content = match.Groups[5].Value;

            // Inherit parent styles or apply new ones from current tag
            bool currentBold = isBold || tagName == "strong" || tagName == "b";
            bool currentItalic = isItalic || tagName == "em" || tagName == "i";
            bool currentUnderline = isUnderline || tagName == "u";
            string? currentColor = color;

            if (!string.IsNullOrEmpty(styleAttr))
            {
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

                var styleMatch = FontStyleStyleRegex().Match(styleAttr);
                if (styleMatch.Success)
                {
                    var style = styleMatch.Groups[1].Value.Trim().ToLower();
                    if (style == "italic")
                        currentItalic = true;
                    else if (style == "normal")
                        currentItalic = false;
                }
            }

            // RECURSIVELY handle nested tags
            FormatHtmlToText(
                textDescriptor,
                content,
                currentBold,
                currentItalic,
                currentUnderline,
                currentColor
            );

            lastIndex = match.Index + match.Length;
        }

        // Add remaining text
        if (lastIndex < input.Length)
        {
            var remainingText = input[lastIndex..];
            var cleanRemaining = HtmlTagRegex().Replace(remainingText, string.Empty);
            if (!string.IsNullOrEmpty(cleanRemaining))
            {
                var span = textDescriptor.Span(cleanRemaining);
                ApplyStyle(span, isBold, isItalic, isUnderline, color);
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

    private static string PreprocessHtml(string? input, string bullet = "")
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        string pText = WebUtility.HtmlDecode(input ?? "");

        // Standardize markdown-style bolding to HTML strong tags for consistency
        pText = MarkdownBoldRegex().Replace(pText, "<strong>$1</strong>");

        // Standardize markdown-style italic to HTML em tags
        pText = MarkdownItalicAsteriskRegex().Replace(pText, "<em>$1</em>");
        pText = MarkdownItalicUnderscoreRegex().Replace(pText, "<em>$1</em>");

        // Handle HR tags - Convert to unique placeholder
        pText = HrTagRegex().Replace(pText, "[[HR]]");

        // Use a placeholder for the checkmark so we can render it as SVG later
        // We'll replace the unicode char or the request for a checkmark bullet with this placeholder
        // Note: We use a space after the placeholder to ensure separation from text
        const string checkmarkPlaceholder = "[[CHECKMARK]]";

        // Handle Lists (<ul><li>...</li></ul>)
        if (pText.Contains("<li>", StringComparison.OrdinalIgnoreCase))
        {
            // Replace <li> with bullet
            if (!string.IsNullOrEmpty(bullet))
            {
                string bulletReplacement = bullet;

                if (bullet.Contains('\u25B8'))
                {
                    // Keep using span for text-based bullets (Right Arrow)
                    bulletReplacement = $"<span style='color:{PrimaryColor}'>\u25B8</span> ";
                }
                else if (bullet.Contains('\u2713')) // ✓ Checkmark
                {
                    // Use special placeholder for Checkmark to render as SVG
                    bulletReplacement = checkmarkPlaceholder;
                }

                pText = LiOpenTagRegex().Replace(pText, bulletReplacement);
            }
            else
            {
                pText = LiOpenTagRegex().Replace(pText, "• ");
            }

            // Replace </li> with newline
            pText = LiCloseTagRegex().Replace(pText, "\n");
            pText = ListTagRegex().Replace(pText, "");
            pText = pText.Replace("&#8226;", "• ");
        }
        else if (
            !string.IsNullOrEmpty(bullet)
            && pText.Contains('\n')
            && !pText.Contains("<p>", StringComparison.OrdinalIgnoreCase)
        )
        {
            // Plain text with newlines - convert to bullets if requested
            var lines = pText.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();

            string bulletReplacement = bullet;
            if (bullet.Contains('\u25B8')) // ▸ Right Arrow
            {
                bulletReplacement = $"<span style='color:{PrimaryColor}'>\u25B8</span> ";
            }
            else if (bullet.Contains('\u2713')) // ✓ Checkmark
            {
                bulletReplacement = checkmarkPlaceholder;
            }

            foreach (var line in lines)
            {
                var cleanLine = line.Trim().TrimStart('-', '*').Trim();
                if (!string.IsNullOrEmpty(cleanLine))
                    sb.AppendLine($"{bulletReplacement}{cleanLine}");
            }
            return sb.ToString().Trim();
        }

        pText = BrTagRegex().Replace(pText, "\n");
        // Convert </p> to newline
        pText = PCloseTagRegex().Replace(pText, "\n");
        pText = POpenTagRegex().Replace(pText, "");

        // Decode HTML entities
        // Entities already decoded at top

        // Safety net: Identify standalone occurrences of checkmark chars
        // Replace existing unicode checkmarks in the text with the placeholder
        pText = pText.Replace("\u2713", checkmarkPlaceholder);

        // Handle standalone right arrows (keep as text span)
        pText = pText.Replace("\u25B8", $"<span style='color:{PrimaryColor}'>\u25B8</span>");

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

    // Keep strict StripHtml for titles/names where we want clean text only
    private static string StripHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        return HtmlTagRegex().Replace(input, string.Empty).Trim();
    }

    private static int GetPageCount(byte[] pdfBytes)
    {
        var text = Encoding.Default.GetString(pdfBytes);
        var matches = PageTypeRegex().Matches(text);
        return matches.Count;
    }

    // GeneratedRegex patterns
    [GeneratedRegex(@"<.*?>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"/Type\s*/Page\b", RegexOptions.IgnoreCase)]
    private static partial Regex PageTypeRegex();

    [GeneratedRegex(@"\*\*(.*?)\*\*", RegexOptions.Singleline)]
    private static partial Regex MarkdownBoldRegex();

    [GeneratedRegex(@"\*(.*?)\*", RegexOptions.Singleline)]
    private static partial Regex MarkdownItalicAsteriskRegex();

    [GeneratedRegex(@"_(.*?)_", RegexOptions.Singleline)]
    private static partial Regex MarkdownItalicUnderscoreRegex();

#pragma warning disable SYSLIB1045 // Pattern with backreferences not supported by GeneratedRegex
    private static readonly Regex HtmlTagWithStyleRegexInstance = new(
        @"<(strong|b|em|i|u|span)(?:\s+style\s*=\s*(?:""([^""]*)""|'([^']*)'|([^""'\s>]+)))?\s*>(.+?)</\1>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline
    );
#pragma warning restore SYSLIB1045

    private static Regex HtmlTagWithStyleRegex() => HtmlTagWithStyleRegexInstance;

    [GeneratedRegex(@"color\s*:\s*([^;]+)", RegexOptions.IgnoreCase)]
    private static partial Regex ColorStyleRegex();

    [GeneratedRegex(@"font-weight\s*:\s*([^;]+)", RegexOptions.IgnoreCase)]
    private static partial Regex FontWeightStyleRegex();

    [GeneratedRegex(@"font-style\s*:\s*([^;]+)", RegexOptions.IgnoreCase)]
    private static partial Regex FontStyleStyleRegex();

    [GeneratedRegex(@"<li>", RegexOptions.IgnoreCase)]
    private static partial Regex LiOpenTagRegex();

    [GeneratedRegex(@"</li>", RegexOptions.IgnoreCase)]
    private static partial Regex LiCloseTagRegex();

    [GeneratedRegex(@"<ul>|</ul>|<ol>|</ol>", RegexOptions.IgnoreCase)]
    private static partial Regex ListTagRegex();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BrTagRegex();

    [GeneratedRegex(@"</p>", RegexOptions.IgnoreCase)]
    private static partial Regex PCloseTagRegex();

    [GeneratedRegex(@"<p.*?>", RegexOptions.IgnoreCase)]
    private static partial Regex POpenTagRegex();

    [GeneratedRegex(@"<hr\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex HrTagRegex();

    /// <summary>
    /// Renders text with HTML and Markdown support into a TextDescriptor.
    /// Used for single-line or inline candidate-provided fields.
    /// </summary>
    private static void ComposeMarkdownText(TextDescriptor t, string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;
        FormatHtmlToText(t, PreprocessHtml(content));
    }

    /// <summary>
    /// Renders HTML content into a ColumnDescriptor, handling text blocks, horizontal lines, and SVG checkmarks.
    /// Replaces direct Text() calls to support block-level elements like HR and complex Layouts for Checkmarks.
    /// </summary>
    private static void ComposeHtmlContent(
        ColumnDescriptor column,
        string? input,
        float fontSize,
        string fontColor,
        string bullet = ""
    )
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        // Preprocess will convert bullets/checkmarks to placeholders if needed
        var preprocessed = PreprocessHtml(input, bullet);
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
                                        s.FontSize(fontSize).FontColor(fontColor).LineHeight(1.5f)
                                    );
                                    FormatHtmlToText(t, segment);
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
                                    $"<svg viewBox=\"0 0 24 24\"><path d=\"{CheckmarkSvgPath}\" fill=\"{AccentColor}\"/></svg>";
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
                                                .LineHeight(1.5f)
                                        );
                                        // Even if segment is empty (e.g. checkmark only), FormatHtmlToText is safe
                                        FormatHtmlToText(t, segment.Trim());
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
                    .LineColor(BorderColor);
            }
        }
    }
}
