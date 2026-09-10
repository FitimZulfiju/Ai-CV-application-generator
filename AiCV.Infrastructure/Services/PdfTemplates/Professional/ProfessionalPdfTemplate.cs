
namespace AiCV.Infrastructure.Services.PdfTemplates.Professional;

public class ProfessionalPdfTemplate(
    IWebHostEnvironment env,
    IStringLocalizer<AicvResources> localizer
    ) : PdfTemplateBase(env, localizer)
{
    protected override AiCV.Application.Common.Models.CvThemeConfig Style => AiCV.Application.Common.Constants.ThemeRegistry.Professional;

    public override void ComposeHeader(IContainer container, CandidateProfile profile)
    {
        bool showPhoto = HasProfilePhoto(profile, out var photoPath);
        var headerBg = Style.PrimaryColor;
        const string headerTextCol = "#ffffff";
        var accentCol = Style.AccentColor;
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
                                e.AlignMiddle()
                                    .AlignLeft()
                                    .Width(photoSize, Unit.Centimetre)
                                    .Height(photoSize, Unit.Centimetre)
                                    .Element(inner => ComposeProfilePhoto(inner, photoPath, photoSize, "#ffffff", 2));
                            });
                    }

                    row.RelativeItem()
                        .AlignCenter()
                        .Column(col =>
                        {
                            ComposeHeaderName(col, profile.FullName ?? "", showPhoto ? 24 : 26, headerTextCol, letterSpacing: -0.02f);
                            ComposeHeaderTitle(col, profile.Title ?? "", showPhoto ? 10f : 11f, titleTextCol, letterSpacing: 0.02f);
                            ComposeHeaderContactRow(col, profile, showPhoto ? 8f : 9f, headerTextCol);
                            ComposeHeaderLinkRow(col, profile, showPhoto ? 8f : 9f, headerTextCol);
                            ComposeHeaderTagline(col, profile.Tagline ?? "", showPhoto ? 8.5f : 9.5f, headerTextCol, titleTextCol);
                        });
                });
        });
    }

    protected override void ComposeSectionTitle(ColumnDescriptor column, string title, string icon, bool hasTopPadding)
    {
        var item = column.Item().PaddingBottom(0.3f, Unit.Centimetre);
        if (hasTopPadding)
        {
            item = item.PaddingTop(0.3f, Unit.Centimetre);
        }
        item.Row(row =>
        {
            row.AutoItem()
                .BorderBottom(1.5f)
                .BorderColor(Style.PrimaryColor)
                .PaddingBottom(2)
                .Text(t =>
                {
                    if (!string.IsNullOrEmpty(icon))
                    {
                        t.Span(icon + " ").FontFamily("Segoe UI Emoji");
                    }
                    t.Span(title.ToUpper())
                        .FontSize(12)
                        .Bold()
                        .FontColor(Style.PrimaryDark)
                        .LetterSpacing(0.06f);
                });
        });
    }
}
