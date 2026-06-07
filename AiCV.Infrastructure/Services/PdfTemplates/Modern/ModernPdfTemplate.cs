namespace AiCV.Infrastructure.Services.PdfTemplates.Modern;

public class ModernPdfTemplate : PdfTemplateBase
{
    protected override bool UseSectionSeparators => true;
    protected override bool CenterLanguageContent => true;
    protected override bool UseInterestChips => true;
    protected override bool UseReferencesFooterPanel => true;
    protected override string SkillsBorderColor => _accentColor;
    protected override string WorkCompanyColor => _accentColor;
    protected override string EducationBorderColor => _accentColor;

    public ModernPdfTemplate(IWebHostEnvironment env, IStringLocalizer<AicvResources> localizer)
        : base(env, localizer)
    {
        _primaryColor = "#2c3e50";
        _primaryDark = "#1a252f";
        _accentColor = "#e67e22";
        _textDark = "#2c3e50";
        _textMedium = "#4b5563";
        _backgroundLight = "#f8f9fa";
        _borderColor = "#dee2e6";
    }

    public override void ComposeHeader(IContainer container, CandidateProfile profile)
    {
        bool showPhoto =
            profile.ShowProfilePicture && !string.IsNullOrEmpty(profile.ProfilePictureUrl);
        var headerBg = _primaryColor;
        const string headerTextCol = "#ffffff";
        var accentCol = _accentColor;
        var titleTextCol = _accentColor;

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
                    const float photoSize = 2.2f;
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
                                        x.FontSize(showPhoto ? 28 : 32)
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
                                        x.FontSize(showPhoto ? 8.5f : 9.5f)
                                            .Bold()
                                            .FontColor(titleTextCol)
                                            .LetterSpacing(0.02f)
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

    protected override void SectionTitle(ColumnDescriptor column, string title)
    {
        column
            .Item()
            .PaddingBottom(0.3f, Unit.Centimetre)
            .PaddingTop(0.3f, Unit.Centimetre)
            .BorderLeft(4f)
            .BorderColor(_accentColor)
            .PaddingLeft(10)
            .Row(row =>
            {
                row.AutoItem()
                    .BorderBottom(1.5f)
                    .BorderColor(_primaryColor)
                    .PaddingBottom(2)
                    .Text(title.ToUpper())
                    .FontSize(12)
                    .Bold()
                    .FontColor(_primaryColor)
                    .LetterSpacing(0.06f);
            });
    }

    protected override void SectionTitleAfterSeparator(ColumnDescriptor column, string title)
    {
        column
            .Item()
            .PaddingBottom(0.3f, Unit.Centimetre)
            .BorderLeft(4f)
            .BorderColor(_accentColor)
            .PaddingLeft(10)
            .Row(row =>
            {
                row.AutoItem()
                    .BorderBottom(1.5f)
                    .BorderColor(_primaryColor)
                    .PaddingBottom(2)
                    .Text(title.ToUpper())
                    .FontSize(12)
                    .Bold()
                    .FontColor(_primaryColor)
                    .LetterSpacing(0.06f);
            });
    }

}
