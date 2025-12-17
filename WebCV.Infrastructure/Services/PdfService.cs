namespace WebCV.Infrastructure.Services;

public partial class PdfService(IWebHostEnvironment env) : IPdfService
{
    private readonly IWebHostEnvironment _env = env;

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
                                    // Date
                                    letterCol
                                        .Item()
                                        .Text(DateTime.Now.ToString("MMMM dd, yyyy"))
                                        .FontSize(size);
                                    letterCol.Item().PaddingBottom(0.8f, Unit.Centimetre);

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
                                .Text(profile.FullName)
                                .FontSize(20)
                                .Bold()
                                .FontColor("#ffffff");

                            // Title
                            col.Item()
                                .AlignCenter()
                                .Text(profile.Title ?? "Candidate")
                                .FontSize(11)
                                .FontColor(Colors.Grey.Lighten4);

                            // Contact Info
                            col.Item()
                                .PaddingTop(0.2f, Unit.Centimetre)
                                .AlignCenter()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(x => x.FontColor("#ffffff").FontSize(9));
                                    var parts = new List<string>();
                                    if (!string.IsNullOrEmpty(profile.Email))
                                        parts.Add($"Email: {profile.Email}");
                                    if (!string.IsNullOrEmpty(profile.PhoneNumber))
                                        parts.Add($"Phone: {profile.PhoneNumber}");
                                    if (!string.IsNullOrEmpty(profile.Location))
                                        parts.Add($"Location: {profile.Location}");

                                    t.Span(string.Join(" | ", parts));
                                });

                            // Links
                            col.Item()
                                .PaddingTop(0.1f, Unit.Centimetre)
                                .AlignCenter()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(x => x.FontColor("#ffffff").FontSize(9));
                                    var parts = new List<string>();
                                    if (!string.IsNullOrEmpty(profile.LinkedInUrl))
                                        parts.Add($"LinkedIn: {profile.LinkedInUrl}");
                                    if (!string.IsNullOrEmpty(profile.PortfolioUrl))
                                        parts.Add($"GitHub: {profile.PortfolioUrl}");

                                    t.Span(string.Join(" | ", parts));
                                });
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

    private static void ComposePageOne(
        IContainer container,
        CandidateProfile profile,
        float fontSize
    )
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
                        c.Item()
                            .Text(t =>
                            {
                                t.DefaultTextStyle(s =>
                                    s.FontColor(TextMedium).FontSize(fontSize).LineHeight(1.5f)
                                );
                                FormatHtmlToText(t, PreprocessHtml(profile.ProfessionalSummary));
                            });
                    });
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }

            // Skills (Core Competencies)
            if (profile.Skills != null && profile.Skills.Count != 0)
            {
                SectionTitle(col, "Core Competencies");

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
                                .Text(StripHtml(cat.Key))
                                .Bold()
                                .FontSize(fontSize)
                                .FontColor(PrimaryDark);
                            c.Item()
                                .Text(
                                    string.Join(", ", cat.Select(s => StripHtml(s.Name)).Distinct())
                                )
                                .FontSize(fontSize - 1)
                                .FontColor(TextMedium);
                        });
                }
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }
        });
    }

    private static void ComposePageTwo(
        IContainer container,
        CandidateProfile profile,
        float fontSize
    )
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(1, Unit.Centimetre);

            // Experience
            if (profile.WorkExperience != null && profile.WorkExperience.Count != 0)
            {
                SectionTitle(col, "Work Experience");

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
                                .Text(StripHtml(exp.JobTitle ?? ""))
                                .Bold()
                                .FontSize(fontSize + 1)
                                .FontColor(TextDark);
                            table
                                .Cell()
                                .AlignRight()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(s =>
                                        s.FontSize(fontSize - 2).FontColor(TextMedium)
                                    );
                                    t.Span(
                                        $"{exp.StartDate:MM/yyyy} – {(exp.EndDate.HasValue ? exp.EndDate.Value.ToString("MM/yyyy") : "Present")}"
                                    );
                                    var duration = CalculateDuration(exp.StartDate, exp.EndDate);
                                    if (!string.IsNullOrEmpty(duration))
                                    {
                                        t.Span($" ({duration})").Italic();
                                    }
                                });

                            // Row 2: Company
                            var companyText = StripHtml(exp.CompanyName ?? "");
                            if (!string.IsNullOrEmpty(exp.Location))
                                companyText += $" - {StripHtml(exp.Location)}";
                            table
                                .Cell()
                                .ColumnSpan(2)
                                .Text(companyText)
                                .FontSize(fontSize)
                                .FontColor(PrimaryColor)
                                .SemiBold();

                            // Row 3: Description with Bullets
                            if (!string.IsNullOrWhiteSpace(exp.Description))
                            {
                                table
                                    .Cell()
                                    .ColumnSpan(2)
                                    .PaddingTop(0.2f, Unit.Centimetre)
                                    .Text(t =>
                                    {
                                        t.DefaultTextStyle(dt =>
                                            dt.FontSize(fontSize - 1)
                                                .FontColor(TextMedium)
                                                .LineHeight(1.5f)
                                        );
                                        FormatHtmlToText(
                                            t,
                                            PreprocessHtml(exp.Description, "\u25B8 ")
                                        );
                                    });
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

    private static void ComposePageThree(
        IContainer container,
        CandidateProfile profile,
        float fontSize
    )
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(1, Unit.Centimetre);

            // Education
            if (profile.Educations != null && profile.Educations.Count != 0)
            {
                SectionTitle(col, "Education");

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
                                                        .Text(StripHtml(edu.Degree ?? ""))
                                                        .Bold()
                                                        .FontSize(fontSize + 1)
                                                        .FontColor(TextDark);
                                                    r.ConstantItem(100)
                                                        .AlignRight()
                                                        .Text(
                                                            $"{edu.StartDate:yyyy} - {(edu.EndDate.HasValue ? edu.EndDate.Value.ToString("yyyy") : "Present")}"
                                                        )
                                                        .FontSize(fontSize - 2)
                                                        .FontColor(TextMedium);
                                                });

                                            c.Item()
                                                .Text(StripHtml(edu.InstitutionName ?? ""))
                                                .FontSize(fontSize)
                                                .FontColor(PrimaryColor)
                                                .SemiBold()
                                                .Bold();

                                            if (!string.IsNullOrEmpty(edu.Description))
                                            {
                                                // Separator line (matches CSS .recognition border-top)
                                                c.Item()
                                                    .PaddingTop(0.25f, Unit.Centimetre)
                                                    .LineHorizontal(1)
                                                    .LineColor(BorderColor);

                                                // Handle HTML tags like <strong style=color:blue;font-weight:normal;>
                                                c.Item()
                                                    .PaddingTop(0.25f, Unit.Centimetre)
                                                    .Text(t =>
                                                    {
                                                        t.DefaultTextStyle(s =>
                                                            s.FontSize(fontSize - 1)
                                                                .FontColor(TextMedium)
                                                                .LineHeight(1.5f)
                                                        );
                                                        FormatHtmlToText(
                                                            t,
                                                            PreprocessHtml(edu.Description)
                                                        );
                                                    });
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
                SectionTitle(col, "Personal Projects");

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
                                                        .Text(StripHtml(proj.Name ?? ""))
                                                        .Bold()
                                                        .FontSize(fontSize + 1)
                                                        .FontColor(TextDark);

                                                    var dateStr = "";
                                                    if (proj.StartDate.HasValue)
                                                    {
                                                        dateStr =
                                                            $"{proj.StartDate.Value:yyyy} - {(proj.EndDate.HasValue ? proj.EndDate.Value.ToString("yyyy") : "Present")}";
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
                                                            t.Span("GitHub: ")
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
                                                            t.Span("Technologies: ")
                                                                .Bold()
                                                                .FontColor(PrimaryDark);
                                                            t.Span(StripHtml(proj.Technologies));
                                                        }
                                                    });
                                            }

                                            if (!string.IsNullOrEmpty(proj.Role))
                                            {
                                                c.Item()
                                                    .Text(StripHtml(proj.Role))
                                                    .FontSize(fontSize)
                                                    .FontColor(PrimaryColor)
                                                    .SemiBold();
                                            }

                                            if (!string.IsNullOrEmpty(proj.Description))
                                            {
                                                // CSS uses ✓ (U+2713) for project features
                                                c.Item()
                                                    .PaddingTop(0.1f, Unit.Centimetre)
                                                    .Text(t =>
                                                    {
                                                        t.DefaultTextStyle(dt =>
                                                            dt.FontSize(fontSize - 1)
                                                                .FontColor(TextMedium)
                                                                .LineHeight(1.5f)
                                                        );
                                                        FormatHtmlToText(
                                                            t,
                                                            PreprocessHtml(
                                                                proj.Description,
                                                                "\u2713 "
                                                            )
                                                        );
                                                    });
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
                SectionTitle(col, "Languages");
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
                            t.Span(StripHtml(lang.Name));
                            t.Span($" ({StripHtml(lang.Proficiency)})").FontSize(8).Italic();
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
                SectionTitle(col, "Interests");

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
                                        .Text(StripHtml(interest.Name))
                                        .FontSize(8)
                                        .FontColor(TextMedium);
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
                .Text("References available upon request")
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

    private static void FormatHtmlToText(TextDescriptor textDescriptor, string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        // Use generated regex to match HTML tags with optional inline styles
        var regex = HtmlTagWithStyleRegex();

        int lastIndex = 0;
        foreach (Match match in regex.Matches(input))
        {
            // Add text before this match (strip any remaining HTML tags)
            if (match.Index > lastIndex)
            {
                var beforeText = input[lastIndex..match.Index];
                var cleanBefore = HtmlTagRegex().Replace(beforeText, string.Empty);
                if (!string.IsNullOrEmpty(cleanBefore))
                {
                    textDescriptor.Span(cleanBefore);
                }
            }

            var tagName = match.Groups[1].Value.ToLower();
            // Style attr can be in group 2 (double), 3 (single), or 4 (unquoted)
            var styleAttr = match.Groups[2].Value + match.Groups[3].Value + match.Groups[4].Value;
            var content = match.Groups[5].Value;

            // Strip nested HTML from content (simple handling)
            var cleanContent = HtmlTagRegex().Replace(content, string.Empty);

            // Initial state based on tag
            bool isBold = tagName == "strong" || tagName == "b";
            bool isItalic = tagName == "em" || tagName == "i";
            string? color = null;

            if (!string.IsNullOrEmpty(styleAttr))
            {
                // Parse color
                var colorMatch = ColorStyleRegex().Match(styleAttr);
                if (colorMatch.Success)
                {
                    var rawColor = colorMatch.Groups[1].Value.Trim();
                    color = GetHexColor(rawColor);
                }

                // Parse font-weight
                var weightMatch = FontWeightStyleRegex().Match(styleAttr);
                if (weightMatch.Success)
                {
                    var weight = weightMatch.Groups[1].Value.Trim().ToLower();
                    if (weight == "bold" || weight == "700" || weight == "800" || weight == "900")
                        isBold = true;
                    if (weight == "normal" || weight == "400")
                        isBold = false;
                }

                // Parse font-style
                var styleMatch = FontStyleStyleRegex().Match(styleAttr);
                if (styleMatch.Success)
                {
                    var style = styleMatch.Groups[1].Value.Trim().ToLower();
                    if (style == "italic")
                        isItalic = true;
                    if (style == "normal")
                        isItalic = false;
                }
            }

            // Apply styling
            if (cleanContent == "\u25B8") // Arrow
            {
                textDescriptor.Element(e =>
                    e.PaddingBottom(-1.5f)
                        .Width(10)
                        .Height(10)
                        .Svg(
                            $"<svg viewBox=\"0 0 24 24\"><path fill=\"{PrimaryColor}\" d=\"M9 12l-5 5V7z\"/></svg>"
                        )
                );
            }
            else if (cleanContent == "\u2713") // Checkmark
            {
                textDescriptor.Element(e =>
                    e.PaddingBottom(-1)
                        .Width(10)
                        .Height(10)
                        .Svg(
                            $"<svg viewBox=\"0 0 24 24\"><path fill=\"{AccentColor}\" d=\"M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z\"/></svg>"
                        )
                );

                // Add a small space after
                textDescriptor.Span(" ");
            }
            else
            {
                var span = textDescriptor.Span(cleanContent);
                if (isBold)
                    span.Bold();
                if (isItalic)
                    span.Italic();
                if (!string.IsNullOrEmpty(color))
                    span.FontColor(color);
            }

            lastIndex = match.Index + match.Length;
        }

        // Add remaining text
        if (lastIndex < input.Length)
        {
            var remainingText = input[lastIndex..];
            var cleanRemaining = HtmlTagRegex().Replace(remainingText, string.Empty);
            if (!string.IsNullOrEmpty(cleanRemaining))
            {
                textDescriptor.Span(cleanRemaining);
            }
        }
    }

    private static string PreprocessHtml(string? input, string bullet = "")
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        string pText = input ?? "";

        // Handle Lists (<ul><li>...</li></ul>)
        if (pText.Contains("<li>", StringComparison.OrdinalIgnoreCase))
        {
            // Replace <li> with bullet
            if (!string.IsNullOrEmpty(bullet))
            {
                // Wrap bullet in color span if it's the right arrow or checkmark
                string coloredBullet = bullet;
                if (bullet.Contains('\u25B8')) // ▸ Right Arrow
                {
                    coloredBullet = $"<span style='color:{PrimaryColor}'>\u25B8</span> ";
                }
                else if (bullet.Contains('\u2713')) // ✓ Checkmark
                {
                    coloredBullet = $"<span style='color:{AccentColor}'>\u2713</span> ";
                }

                pText = LiOpenTagRegex().Replace(pText, coloredBullet);
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

            // Wrap bullet in color span
            string coloredBullet = bullet;
            if (bullet.Contains('\u25B8')) // ▸ Right Arrow
            {
                coloredBullet = $"<span style='color:{PrimaryColor}'>\u25B8</span> ";
            }
            else if (bullet.Contains('\u2713')) // ✓ Checkmark
            {
                coloredBullet = $"<span style='color:{AccentColor}'>\u2713</span> ";
            }

            foreach (var line in lines)
            {
                var cleanLine = line.Trim().TrimStart('-', '*').Trim();
                if (!string.IsNullOrEmpty(cleanLine))
                    sb.AppendLine($"{coloredBullet}{cleanLine}");
            }
            return sb.ToString().Trim();
        }

        pText = BrTagRegex().Replace(pText, "\n");
        // Convert </p> to newline
        pText = PCloseTagRegex().Replace(pText, "\n");
        pText = POpenTagRegex().Replace(pText, "");

        // Decode HTML entities
        pText = System.Net.WebUtility.HtmlDecode(pText);

        // Safety net: Identify and colorize standalone occurrences of these chars if they weren't caught above
        // This handles cases where the text itself already contains the chars (not just replacing LIs)
        pText = pText.Replace("\u25B8", $"<span style='color:{PrimaryColor}'>\u25B8</span>");
        pText = pText.Replace("\u2713", $"<span style='color:{AccentColor}'>\u2713</span>");

        return pText.Trim();
    }

    private static string CalculateDuration(DateTime? start, DateTime? end)
    {
        if (!start.HasValue)
            return "";

        var endDate = end ?? DateTime.Now;
        var totalMonths =
            ((endDate.Year - start.Value.Year) * 12) + endDate.Month - start.Value.Month + 1;

        var years = totalMonths / 12;
        var months = totalMonths % 12;

        var parts = new List<string>();
        if (years > 0)
            parts.Add($"{years} year{(years > 1 ? "s" : "")}");
        if (months > 0)
            parts.Add($"{months} month{(months > 1 ? "s" : "")}");

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

    // Backreference \1 is not supported by GeneratedRegex, use compiled Regex instead
#pragma warning disable SYSLIB1045 // Cannot use GeneratedRegex - pattern uses backreference \1
    private static readonly Regex HtmlTagWithStyleRegexInstance = new(
        @"<(strong|b|em|i|u|span)(?:\s+style\s*=\s*(?:""([^""]*)""|'([^']*)'|([^""'\s>]+)))?\s*>(.+?)</\1>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
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
}
