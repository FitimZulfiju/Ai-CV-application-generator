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
    protected virtual bool UseSectionSeparators => false;
    protected virtual bool CenterLanguageContent => false;
    protected virtual bool UseInterestChips => false;
    protected virtual bool UseReferencesFooterPanel => false;
    protected virtual string AdditionalSectionBorderColor => _primaryColor;
    protected virtual string SummaryBorderColor => _primaryColor;
    protected virtual string SkillsBorderColor => _primaryColor;
    protected virtual string WorkCompanyColor => _primaryColor;
    protected virtual bool SuppressWorkDescriptionBullet => false;
    protected virtual string EducationBorderColor => _accentColor;
    protected virtual string CoverLetterBorderColor => _primaryColor;

    protected const string CheckmarkSvgPath = "M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z";

    public abstract void ComposeHeader(IContainer container, CandidateProfile profile);
    public virtual void ComposePageOne(
        IContainer container,
        CandidateProfile profile,
        float fontSize
    )
    {
        ComposeSummaryAndSkills(container, profile, fontSize);
    }

    public virtual void ComposePageTwo(
        IContainer container,
        CandidateProfile profile,
        float fontSize
    )
    {
        ComposeWorkExperienceSection(container, profile, fontSize);
    }

    public virtual void ComposePageThree(
        IContainer container,
        CandidateProfile profile,
        float fontSize
    )
    {
        ComposeEducationAndAdditionalSections(container, profile, fontSize);
    }

    public virtual void ComposeCoverLetter(
        IContainer container,
        string letterContent,
        CandidateProfile profile,
        float fontSize
    )
    {
        ComposeCoverLetterPanel(container, letterContent, fontSize);
    }

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

    protected virtual void SectionTitleAfterSeparator(ColumnDescriptor column, string title)
    {
        SectionTitle(column, title);
    }

    protected void SectionSeparator(ColumnDescriptor column)
    {
        column
            .Item()
            .PaddingTop(0.4f, Unit.Centimetre)
            .PaddingBottom(0.4f, Unit.Centimetre)
            .LineHorizontal(1)
            .LineColor(_borderColor);
    }

    protected void ComposePageThreeAdditionalSections(
        ColumnDescriptor column,
        CandidateProfile profile,
        float fontSize
    )
    {
        var renderModel = CvRenderModel.FromProfile(profile, _localizer);

        if (renderModel.Projects.Count != 0)
        {
            if (UseSectionSeparators)
                SectionSeparator(column);

            if (UseSectionSeparators)
                SectionTitleAfterSeparator(column, _localizer["PersonalProjectsCv"]);
            else
                SectionTitle(column, _localizer["PersonalProjectsCv"]);
            var projectList = renderModel.Projects;
            for (int i = 0; i < projectList.Count; i++)
            {
                var project = projectList[i];
                column
                    .Item()
                    .PaddingBottom(i < projectList.Count - 1 ? 0.3f : 0f, Unit.Centimetre)
                    .Element(cell =>
                    {
                        cell.Background(_backgroundLight)
                            .BorderLeft(1.5f)
                            .BorderColor(AdditionalSectionBorderColor)
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
                                                ComposeMarkdownText(t, project.Name ?? "");
                                            });
                                        r.ConstantItem(100)
                                            .AlignRight()
                                            .Text(project.DateRange)
                                            .FontSize(fontSize - 2)
                                            .FontColor(_textMedium);
                                    });

                                if (!string.IsNullOrWhiteSpace(project.Link))
                                {
                                    c.Item()
                                        .PaddingTop(0.15f, Unit.Centimetre)
                                        .Text(t =>
                                        {
                                            t.DefaultTextStyle(x =>
                                                x.FontSize(fontSize - 1)
                                                    .FontColor(_primaryColor)
                                                    .SemiBold()
                                            );
                                            t.Span($"{_localizer["GitHubLabel"]} ");
                                            ComposeMarkdownText(
                                                t,
                                                $"<a href='{project.Link}'>{project.Link}</a>",
                                                _primaryColor
                                            );
                                        });
                                }

                                if (!string.IsNullOrWhiteSpace(project.Technologies))
                                {
                                    c.Item()
                                        .PaddingTop(0.1f, Unit.Centimetre)
                                        .Text(t =>
                                        {
                                            t.DefaultTextStyle(x =>
                                                x.FontSize(fontSize - 1)
                                                    .FontColor(_textMedium)
                                            );
                                            t.Span($"{_localizer["TechnologiesLabel"]} ");
                                            ComposeMarkdownText(t, project.Technologies);
                                        });
                                }

                                if (!string.IsNullOrWhiteSpace(project.Role))
                                {
                                    c.Item()
                                        .PaddingTop(0.1f, Unit.Centimetre)
                                        .Text(t =>
                                        {
                                            t.DefaultTextStyle(x =>
                                                x.FontSize(fontSize - 1)
                                                    .FontColor(_textMedium)
                                                    .Italic()
                                            );
                                            ComposeMarkdownText(t, project.Role);
                                        });
                                }

                                if (!string.IsNullOrWhiteSpace(project.Description))
                                {
                                    c.Item()
                                        .PaddingTop(0.15f, Unit.Centimetre)
                                        .Column(c2 =>
                                            ComposeHtmlContent(
                                                c2,
                                                project.Description,
                                                fontSize - 1,
                                                _textMedium
                                            )
                                        );
                                }

                                ComposeProjectSectionDetails(c, project, fontSize);
                            });
                    });
            }

        }

        if (renderModel.Languages.Count != 0)
        {
            if (UseSectionSeparators)
                SectionSeparator(column);

            if (UseSectionSeparators)
                SectionTitleAfterSeparator(column, _localizer["LanguagesCv"]);
            else
                SectionTitle(column, _localizer["LanguagesCv"]);
            var languageItem = column
                .Item()
                .Background(_backgroundLight)
                .BorderLeft(1.5f)
                .BorderColor(AdditionalSectionBorderColor)
                .CornerRadius(5)
                .Padding(10);

            if (CenterLanguageContent)
                languageItem = languageItem.AlignCenter();

            languageItem.Text(t =>
            {
                if (CenterLanguageContent)
                    t.AlignCenter();

                t.DefaultTextStyle(x => x.FontSize(fontSize - 1).FontColor(_textMedium));
                for (int i = 0; i < renderModel.Languages.Count; i++)
                {
                    var language = renderModel.Languages[i];
                    if (i > 0)
                        t.Span(" • ").FontColor(_textDark);

                    ComposeMarkdownText(t, language.Name ?? "");

                    if (!string.IsNullOrWhiteSpace(language.Proficiency))
                    {
                        t.Span(" ");
                        ComposeMarkdownText(t, language.Proficiency, _textDark);
                    }
                }
            });

        }

        if (renderModel.Interests.Count != 0)
        {
            if (UseSectionSeparators)
                SectionSeparator(column);

            if (UseSectionSeparators)
                SectionTitleAfterSeparator(column, _localizer["InterestsCv"]);
            else
                SectionTitle(column, _localizer["InterestsCv"]);
            var interestItem = column
                .Item()
                .PaddingTop(0.1f, Unit.Centimetre)
                .Background(_backgroundLight)
                .BorderLeft(1.5f)
                .BorderColor(AdditionalSectionBorderColor)
                .CornerRadius(5)
                .Padding(10);

            if (UseInterestChips)
                interestItem = interestItem.AlignCenter();

            interestItem.Text(t =>
            {
                if (UseInterestChips)
                    t.AlignCenter();

                t.DefaultTextStyle(x => x.FontSize(fontSize - 1).FontColor(_textMedium));
                for (int i = 0; i < renderModel.Interests.Count; i++)
                {
                    if (i > 0)
                        t.Span(" • ").FontColor(_textDark);

                    ComposeMarkdownText(t, renderModel.Interests[i].Name ?? "");
                }
            });
        }

        var referencesItem = column.Item().ExtendVertical().AlignBottom();

        if (UseReferencesFooterPanel)
        {
            referencesItem
                .Background(_backgroundLight)
                .BorderTop(1)
                .BorderColor(_borderColor)
                .PaddingVertical(0.18f, Unit.Centimetre)
                .PaddingHorizontal(0.4f, Unit.Centimetre)
                .AlignCenter()
                .Text(t =>
                {
                    t.AlignCenter();
                    t.DefaultTextStyle(x =>
                        x.FontSize(fontSize - 2).Italic().FontColor(_textMedium)
                    );
                    ComposeMarkdownText(t, _localizer["ReferencesAvailableUponRequest"]);
                });
        }
        else
        {
            referencesItem
                .AlignCenter()
                .Text(t =>
                {
                    t.AlignCenter();
                    t.DefaultTextStyle(x =>
                        x.FontSize(fontSize - 2).Italic().FontColor(_textMedium)
                    );
                    ComposeMarkdownText(t, _localizer["ReferencesAvailableUponRequest"]);
                });
        }
    }

    protected void ComposeSummaryAndSkills(
        IContainer container,
        CandidateProfile profile,
        float fontSize
    )
    {
        var renderModel = CvRenderModel.FromProfile(profile, _localizer);

        container.Column(col =>
        {
            col.Item().PaddingTop(1, Unit.Centimetre);
            if (!string.IsNullOrWhiteSpace(profile.ProfessionalSummary))
            {
                col.Item()
                    .Background(_backgroundLight)
                    .BorderLeft(1.5f)
                    .BorderColor(SummaryBorderColor)
                    .CornerRadius(5)
                    .Padding(10)
                    .Column(c =>
                        ComposeHtmlContent(c, profile.ProfessionalSummary, fontSize, _textMedium)
                    );
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }

            if (renderModel.SkillGroups.Count != 0)
            {
                SectionTitle(col, _localizer["CoreCompetencies"]);
                foreach (var skillGroup in renderModel.SkillGroups)
                {
                    col.Item()
                        .PaddingBottom(0.3f, Unit.Centimetre)
                        .Background(_backgroundLight)
                        .BorderLeft(1.5f)
                        .BorderColor(SkillsBorderColor)
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
                                    ComposeMarkdownText(t, skillGroup.Category);
                                });
                            c.Item()
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(x =>
                                        x.FontSize(fontSize - 1).FontColor(_textMedium)
                                    );
                                    ComposeMarkdownText(t, skillGroup.SkillNames);
                                });
                        });
                }
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }
        });
    }

    protected void ComposeWorkExperienceSection(
        IContainer container,
        CandidateProfile profile,
        float fontSize
    )
    {
        var renderModel = CvRenderModel.FromProfile(profile, _localizer);

        container.Column(col =>
        {
            col.Item().PaddingTop(1, Unit.Centimetre);
            if (renderModel.WorkExperiences.Count != 0)
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
                        var workExperiences = renderModel.WorkExperiences;
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
                                    t.Span(exp.DateRange);
                                    if (!string.IsNullOrEmpty(exp.Duration))
                                        t.Span($" ({exp.Duration})").Italic();
                                });
                            table
                                .Cell()
                                .ColumnSpan(2)
                                .Text(t =>
                                {
                                    t.DefaultTextStyle(x =>
                                        x.FontSize(fontSize).FontColor(WorkCompanyColor).SemiBold()
                                    );
                                    ComposeMarkdownText(t, exp.CompanyLine);
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
                                            bullet: SuppressWorkDescriptionBullet ? null : "•"
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

    protected void ComposeEducationAndAdditionalSections(
        IContainer container,
        CandidateProfile profile,
        float fontSize
    )
    {
        var renderModel = CvRenderModel.FromProfile(profile, _localizer);

        container.Column(col =>
        {
            col.Item().PaddingTop(1, Unit.Centimetre);
            if (renderModel.Educations.Count != 0)
            {
                SectionTitle(col, _localizer["EducationCv"]);
                col.Item()
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns => columns.RelativeColumn());
                        var eduList = renderModel.Educations;
                        for (int i = 0; i < eduList.Count; i++)
                        {
                            var edu = eduList[i];
                            table
                                .Cell()
                                .Element(cell =>
                                {
                                    cell.Background(_backgroundLight)
                                        .BorderLeft(1.5f)
                                        .BorderColor(EducationBorderColor)
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
                                                        .Text(edu.DateRange)
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

    protected void ComposeCoverLetterPanel(
        IContainer container,
        string letterContent,
        float fontSize
    )
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(0.8f, Unit.Centimetre);
            col.Item()
                .Background(_backgroundLight)
                .BorderLeft(1.5f)
                .BorderColor(CoverLetterBorderColor)
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

    private void ComposeProjectSectionDetails(
        ColumnDescriptor column,
        CvProjectItem project,
        float fontSize
    )
    {
        if (string.IsNullOrWhiteSpace(project.Section.Header) && string.IsNullOrWhiteSpace(project.Section.Details))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(project.Section.Header))
        {
            column
                .Item()
                .PaddingTop(0.15f, Unit.Centimetre)
                .Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(fontSize - 1).Bold().FontColor(_primaryColor));
                    ComposeMarkdownText(t, project.Section.Header);
                });
        }

        if (!string.IsNullOrWhiteSpace(project.Section.Details))
        {
            column
                .Item()
                .PaddingTop(0.1f, Unit.Centimetre)
                .Column(c =>
                    ComposeHtmlContent(c, project.Section.Details, fontSize - 1, _textMedium)
                );
        }
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
                var cleanBefore = WebUtility.HtmlDecode(
                    HtmlTagRegex().Replace(beforeText, string.Empty)
                );
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
            var cleanRemaining = WebUtility.HtmlDecode(
                HtmlTagRegex().Replace(remainingText, string.Empty)
            );
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "SYSLIB1045:Use GeneratedRegexAttribute to generate the regular expression implementation at compile-time",
        Justification = "GeneratedRegex cannot fully generate this pattern because it uses a backreference in the closing tag."
    )]
    private static readonly Regex HtmlTagWithStyleRegexInstance = new(
        @"<(strong|b|em|i|u|span|div|p|h1|h2|h3|h4|h5|h6|a|blockquote|code|pre)(?:\s+([^>]*?))?\s*>(.+?)</\1>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline
    );

    protected static Regex HtmlTagWithStyleRegex() => HtmlTagWithStyleRegexInstance;
}
