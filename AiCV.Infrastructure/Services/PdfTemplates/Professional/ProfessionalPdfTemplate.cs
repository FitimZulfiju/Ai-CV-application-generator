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
    protected override bool SuppressWorkDescriptionBullet => true;

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

}
