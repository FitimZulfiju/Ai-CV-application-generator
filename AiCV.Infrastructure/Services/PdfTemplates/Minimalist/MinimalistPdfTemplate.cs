namespace AiCV.Infrastructure.Services.PdfTemplates.Minimalist;

public class MinimalistPdfTemplate : PdfTemplateBase
{
    protected override bool UseSectionSeparators => true;
    protected override bool CenterLanguageContent => true;
    protected override bool UseInterestChips => true;
    protected override bool UseReferencesFooterPanel => true;
    protected override string AdditionalSectionBorderColor => _borderColor;
    protected override string SummaryBorderColor => _borderColor;
    protected override string SkillsBorderColor => _borderColor;
    protected override string EducationBorderColor => _borderColor;
    protected override string CoverLetterBorderColor => _borderColor;

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

}
