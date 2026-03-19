namespace AiCV.Infrastructure.Services.PdfTemplates.Minimalist;

public class MinimalistPdfTemplate : PdfTemplateBase
{
    protected override bool UseSectionSeparators => true;
    protected override bool CenterLanguageContent => true;
    protected override bool UseInterestChips => true;
    protected override bool UseReferencesFooterPanel => true;

    public MinimalistPdfTemplate(IWebHostEnvironment env, IStringLocalizer<AicvResources> localizer)
        : base(env, localizer)
    {
        _primaryColor = "#333333";
        _primaryDark = "#111111";
        _accentColor = "#777777";
        _textDark = "#111111";
        _textMedium = "#444444";
        _backgroundLight = "#ffffff";
        _borderColor = "#eeeeee";
    }

    public override void ComposeHeader(IContainer container, CandidateProfile profile)
    {
        bool showPhoto =
            profile.ShowProfilePicture && !string.IsNullOrEmpty(profile.ProfilePictureUrl);
        const string headerBg = "#ffffff";
        var headerTextCol = _textDark;
        var accentCol = _borderColor;
        var titleTextCol = _textMedium;

        container.Column(c =>
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
                                    e.Background("#ffffff")
                                        .CornerRadius(1.5f, Unit.Centimetre)
                                        .Border(2)
                                        .BorderColor("#eeeeee")
                                        .Image(path)
                                        .FitArea()
                                );
                        }
                    }

                    col.Item()
                        .AlignCenter()
                        .Text(t =>
                        {
                            t.AlignCenter();
                            t.DefaultTextStyle(x =>
                                x.FontSize(28).Bold().FontColor(headerTextCol).LetterSpacing(0.2f)
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
                            ComposeMarkdownText(t, (profile.Title ?? "").ToUpper(), titleTextCol);
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
                                t.DefaultTextStyle(x => x.FontColor(headerTextCol).FontSize(9.5f));
                                ComposeMarkdownText(t, profile.Tagline, headerTextCol);
                            });
                    }
                });
        });
    }

    protected override void SectionTitle(ColumnDescriptor column, string title)
    {
        column
            .Item()
            .PaddingBottom(0.3f, Unit.Centimetre)
            .PaddingTop(0.3f, Unit.Centimetre)
            .Row(row =>
            {
                row.AutoItem()
                    .Width(17, Unit.Centimetre)
                    .BorderBottom(1.5f)
                    .BorderColor(_primaryDark)
                    .PaddingBottom(2)
                    .Text(title.ToUpper())
                    .FontSize(11)
                    .Bold()
                    .FontColor(_primaryDark)
                    .LetterSpacing(0.15f);
            });
    }

    protected override void SectionTitleAfterSeparator(ColumnDescriptor column, string title)
    {
        column
            .Item()
            .PaddingBottom(0.3f, Unit.Centimetre)
            .Row(row =>
            {
                row.AutoItem()
                    .Width(17, Unit.Centimetre)
                    .BorderBottom(1.5f)
                    .BorderColor(_primaryDark)
                    .PaddingBottom(2)
                    .Text(title.ToUpper())
                    .FontSize(11)
                    .Bold()
                    .FontColor(_primaryDark)
                    .LetterSpacing(0.15f);
            });
    }

    public override void ComposePageOne(
        IContainer container,
        CandidateProfile profile,
        float fontSize
    )
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(1, Unit.Centimetre);
            if (!string.IsNullOrWhiteSpace(profile.ProfessionalSummary))
            {
                col.Item()
                    .Background(_backgroundLight)
                    .BorderLeft(1.5f)
                    .BorderColor(_borderColor)
                    .CornerRadius(5)
                    .Padding(10)
                    .Column(c =>
                        ComposeHtmlContent(c, profile.ProfessionalSummary, fontSize, _textMedium)
                    );
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }

            if (profile.Skills != null && profile.Skills.Count != 0)
            {
                SectionTitle(col, _localizer["CoreCompetencies"]);
                foreach (var cat in profile.Skills.GroupBy(s => s.Category ?? "Other"))
                {
                    col.Item()
                        .PaddingBottom(0.3f, Unit.Centimetre)
                        .Background(_backgroundLight)
                        .BorderLeft(1.5f)
                        .BorderColor(_borderColor)
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
                                    ComposeMarkdownText(
                                        t,
                                        string.Join(", ", cat.Select(s => s.Name).Distinct())
                                    );
                                });
                        });
                }
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }
        });
    }

    public override void ComposePageTwo(
        IContainer container,
        CandidateProfile profile,
        float fontSize
    )
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(1, Unit.Centimetre);
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
                            table
                                .Cell()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(s =>
                                        s.FontSize(fontSize + 1).FontColor(_textDark).Bold()
                                    );
                                    ComposeMarkdownText(t, exp.JobTitle);
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
                                        t.Span($" ({duration})").Italic();
                                });
                            table
                                .Cell()
                                .ColumnSpan(2)
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(x =>
                                        x.FontSize(fontSize).FontColor(_primaryColor).SemiBold()
                                    );
                                    ComposeMarkdownText(
                                        t,
                                        (exp.CompanyName ?? "")
                                            + (
                                                string.IsNullOrEmpty(exp.Location)
                                                    ? ""
                                                    : $" - {exp.Location}"
                                            )
                                    );
                                });
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
                                            _textMedium
                                        )
                                    );
                            }

                            if (i < workExperiences.Count - 1)
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

    public override void ComposePageThree(
        IContainer container,
        CandidateProfile profile,
        float fontSize
    )
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(1, Unit.Centimetre);
            if (profile.Educations != null && profile.Educations.Count != 0)
            {
                SectionTitle(col, _localizer["EducationCv"]);
                col.Item()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns => columns.RelativeColumn());
                        var eduList = profile
                            .Educations.OrderByDescending(e => e.StartDate)
                            .ToList();
                        for (int i = 0; i < eduList.Count; i++)
                        {
                            var edu = eduList[i];
                            table
                                .Cell()
                                .Element(cell =>
                                {
                                    cell.Background(_backgroundLight)
                                        .BorderLeft(1.5f)
                                        .BorderColor(_borderColor)
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
                                                c.Item()
                                                    .PaddingTop(0.25f, Unit.Centimetre)
                                                    .PaddingBottom(0.25f, Unit.Centimetre)
                                                    .LineHorizontal(1)
                                                    .LineColor(_borderColor);
                                                c.Item()
                                                    .Column(cc =>
                                                        ComposeHtmlContent(
                                                            cc,
                                                            edu.Description,
                                                            fontSize - 1,
                                                            _textMedium
                                                        )
                                                    );
                                            }
                                        });
                                });
                            if (i < eduList.Count - 1)
                                table.Cell().ColumnSpan(1).LineHorizontal(1).LineColor("#E0E0E0");
                        }
                    });
            }

            ComposePageThreeAdditionalSections(col, profile, fontSize);
        });
    }

    public override void ComposeCoverLetter(
        IContainer container,
        string letterContent,
        CandidateProfile profile,
        float fontSize
    )
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(0.8f, Unit.Centimetre);
            col.Item()
                .Background(_backgroundLight)
                .BorderLeft(1.5f)
                .BorderColor(_borderColor)
                .CornerRadius(5)
                .Padding(10)
                .Column(letterCol =>
                {
                    if (!string.IsNullOrWhiteSpace(letterContent))
                    {
                        ComposeHtmlContent(
                            letterCol,
                            letterContent,
                            fontSize,
                            _textDark,
                            lineHeight: 1.35f,
                            paragraphSpacing: 8f,
                            preserveParagraphBreaks: true
                        );
                    }
                    else
                    {
                        letterCol.Item().Text("No content provided.").Italic();
                    }
                });
        });
    }
}
