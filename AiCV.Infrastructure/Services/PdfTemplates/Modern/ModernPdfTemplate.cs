using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using AiCV.Domain.Entities;
using System.IO;

namespace AiCV.Infrastructure.Services.PdfTemplates.Modern;

public class ModernPdfTemplate : PdfTemplateBase
{
    private static readonly PdfTemplateStyle _modernStyle = new(
        PrimaryColor: "#2c3e50",
        PrimaryDark: "#1a252f",
        AccentColor: "#e67e22",
        TextDark: "#2c3e50",
        TextMedium: "#4b5563",
        BackgroundLight: "#f8f9fa",
        BorderColor: "#dee2e6",
        UseSectionSeparators: true,
        CenterLanguageContent: true,
        UseInterestChips: true,
        UseReferencesFooterPanel: true,
        SkillsBorderColor: "#e67e22",
        WorkCompanyColor: "#e67e22",
        EducationBorderColor: "#e67e22"
    );

    protected override PdfTemplateStyle Style => _modernStyle;

    public ModernPdfTemplate(IWebHostEnvironment env, IStringLocalizer<AicvResources> localizer)
        : base(env, localizer)
    {
    }

    public override void ComposeHeader(IContainer container, CandidateProfile profile)
    {
        bool showPhoto = HasProfilePhoto(profile, out var photoPath);
        var headerBg = Style.PrimaryColor;
        const string headerTextCol = "#ffffff";
        var accentCol = Style.AccentColor;
        var titleTextCol = Style.AccentColor;

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
                            ComposeHeaderName(col, profile.FullName ?? "", showPhoto ? 28 : 32, headerTextCol, letterSpacing: -0.02f);
                            ComposeHeaderTitle(col, profile.Title ?? "", showPhoto ? 8.5f : 9.5f, titleTextCol, letterSpacing: 0.02f, makeUppercase: true, isBold: true);
                            ComposeHeaderContactRow(col, profile, showPhoto ? 8f : 9f, headerTextCol);
                            ComposeHeaderLinkRow(col, profile, showPhoto ? 8f : 9f, headerTextCol);
                            ComposeHeaderTagline(col, profile.Tagline ?? "", showPhoto ? 8.5f : 9.5f, headerTextCol, titleTextCol);
                        });

                    if (showPhoto)
                        row.ConstantItem(sideWidth, Unit.Centimetre).Element(_ => { });
                });
        });
    }

    protected override void ComposeSectionTitle(ColumnDescriptor column, string title, bool hasTopPadding)
    {
        var item = column.Item().PaddingBottom(0.3f, Unit.Centimetre);
        if (hasTopPadding)
        {
            item = item.PaddingTop(0.3f, Unit.Centimetre);
        }
        item.BorderLeft(4f)
            .BorderColor(Style.AccentColor)
            .PaddingLeft(10)
            .Row(row =>
            {
                row.AutoItem()
                    .BorderBottom(1.5f)
                    .BorderColor(Style.PrimaryColor)
                    .PaddingBottom(2)
                    .Text(title.ToUpper())
                    .FontSize(12)
                    .Bold()
                    .FontColor(Style.PrimaryColor)
                    .LetterSpacing(0.06f);
            });
    }
}
