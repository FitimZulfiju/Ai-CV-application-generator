using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Localization;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using AiCV.Domain.Entities;
using System.IO;

namespace AiCV.Infrastructure.Services.PdfTemplates.Minimalist;

public class MinimalistPdfTemplate : PdfTemplateBase
{
    protected override AiCV.Application.Common.Models.CvThemeConfig Style => AiCV.Application.Common.Constants.ThemeRegistry.Minimalist;

    public MinimalistPdfTemplate(IWebHostEnvironment env, IStringLocalizer<AicvResources> localizer)
        : base(env, localizer)
    {
    }

    public override void ComposeHeader(IContainer container, CandidateProfile profile)
    {
        const string headerBg = "#ffffff";
        var headerTextCol = Style.TextDark;
        var accentCol = Style.BorderColor;
        var titleTextCol = Style.TextMedium;

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
                    if (HasProfilePhoto(profile, out var photoPath))
                    {
                        col.Item()
                            .AlignCenter()
                            .PaddingBottom(0.5f, Unit.Centimetre)
                            .Width(3, Unit.Centimetre)
                            .Height(3, Unit.Centimetre)
                            .Element(e => ComposeProfilePhoto(e, photoPath, 3f, "#eeeeee", 2));
                    }

                    ComposeHeaderName(col, profile.FullName ?? "", 28, headerTextCol, letterSpacing: 0.2f, makeUppercase: true);
                    ComposeHeaderTitle(col, profile.Title ?? "", 10.5f, titleTextCol, letterSpacing: 0.02f, makeUppercase: true);
                    ComposeHeaderContactRow(col, profile, 9, headerTextCol, makeUppercase: true, letterSpacing: 0.05f);
                    ComposeHeaderLinkRow(col, profile, 9, headerTextCol, makeUppercase: true, letterSpacing: 0.05f);
                    ComposeHeaderTagline(col, profile.Tagline ?? "", 9.5f, headerTextCol, titleTextCol);
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
        item.Row(row =>
        {
            row.AutoItem()
                .Width(17, Unit.Centimetre)
                .BorderBottom(1.5f)
                .BorderColor(Style.PrimaryDark)
                .PaddingBottom(2)
                .Text(title.ToUpper())
                .FontSize(11)
                .Bold()
                .FontColor(Style.PrimaryDark)
                .LetterSpacing(0.15f);
        });
    }
}
