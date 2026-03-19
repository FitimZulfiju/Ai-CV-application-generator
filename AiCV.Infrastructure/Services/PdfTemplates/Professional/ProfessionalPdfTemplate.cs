namespace AiCV.Infrastructure.Services.PdfTemplates.Professional;

public class ProfessionalPdfTemplate(
    IWebHostEnvironment env,
    IStringLocalizer<AicvResources> localizer
    ) : PdfTemplateBase(env, localizer)
{
    protected override bool UseSectionSeparators => true;
    protected override bool CenterLanguageContent => true;
    protected override bool UseInterestChips => true;
    protected override bool UseReferencesFooterPanel => true;

    protected override void SectionTitle(ColumnDescriptor column, string title)
    {
        column
            .Item()
            .PaddingBottom(0.3f, Unit.Centimetre)
            .PaddingTop(0.3f, Unit.Centimetre)
            .Row(row =>
            {
                row.AutoItem()
                    .BorderBottom(1.5f)
                    .BorderColor(_primaryColor)
                    .PaddingBottom(2)
                    .Text(title.ToUpper())
                    .FontSize(12)
                    .Bold()
                    .FontColor(_primaryDark)
                    .LetterSpacing(0.06f);
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
                    .BorderBottom(1.5f)
                    .BorderColor(_primaryColor)
                    .PaddingBottom(2)
                    .Text(title.ToUpper())
                    .FontSize(12)
                    .Bold()
                    .FontColor(_primaryDark)
                    .LetterSpacing(0.06f);
            });
    }

    public override void ComposeHeader(IContainer container, CandidateProfile profile)
    {
        bool showPhoto =
            profile.ShowProfilePicture && !string.IsNullOrEmpty(profile.ProfilePictureUrl);
        var headerBg = _primaryColor;
        const string headerTextCol = "#ffffff";
        var accentCol = _accentColor;
        const string titleTextCol = "#F5F5F5";

        container.Column(c =>
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
                    const float photoSize = 2.5f;
                    const float sideWidth = photoSize + 0.5f;

                    if (showPhoto)
                    {
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

                    row.RelativeItem()
                        .AlignCenter()
                        .Column(col =>
                        {
                            col.Item()
                                .AlignCenter()
                                .Text(t =>
                                {
                                    t.AlignCenter();
                                    t.DefaultTextStyle(x =>
                                        x.FontSize(showPhoto ? 24 : 26)
                                            .Bold()
                                            .FontColor(headerTextCol)
                                            .LetterSpacing(-0.02f)
                                    );
                                    ComposeMarkdownText(t, profile.FullName ?? "", headerTextCol);
                                });

                            col.Item()
                                .PaddingTop(0.1f, Unit.Centimetre)
                                .AlignCenter()
                                .Text(t =>
                                {
                                    t.AlignCenter();
                                    t.DefaultTextStyle(x =>
                                        x.FontSize(showPhoto ? 10f : 11f)
                                            .FontColor(titleTextCol)
                                            .LetterSpacing(0.02f)
                                    );
                                    ComposeMarkdownText(t, profile.Title ?? "", titleTextCol);
                                });

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
                        row.ConstantItem(sideWidth, Unit.Centimetre).Element(_ => { });
                });
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
                    .BorderColor(_primaryColor)
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
                        .BorderColor(_primaryColor)
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
                                            _textMedium,
                                            bullet: null
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
                                        .BorderColor(_accentColor)
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
                .BorderColor(_primaryColor)
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
