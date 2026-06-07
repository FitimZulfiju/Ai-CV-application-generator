using AiCV.Application.Common.Models;
using AiCV.Domain;

namespace AiCV.Application.Common.Constants;

public static class ThemeRegistry
{
    public static readonly CvThemeConfig Professional = new(
        ThemeClass: "cv-professional",
        UseSectionSeparators: true,
        CenterLanguageContent: true,
        UseInterestChips: true,
        UseReferencesFooterPanel: true,
        SuppressWorkDescriptionBullet: true
    );

    public static readonly CvThemeConfig Minimalist = new(
        ThemeClass: "cv-minimalist",
        PrimaryColor: "#333333",
        PrimaryDark: "#111111",
        AccentColor: "#777777",
        TextDark: "#111111",
        TextMedium: "#444444",
        BackgroundLight: "#ffffff",
        BorderColor: "#eeeeee",
        UseSectionSeparators: true,
        CenterLanguageContent: true,
        UseInterestChips: true,
        UseReferencesFooterPanel: true,
        AdditionalSectionBorderColor: "#eeeeee",
        SummaryBorderColor: "#eeeeee",
        SkillsBorderColor: "#eeeeee",
        EducationBorderColor: "#eeeeee",
        CoverLetterBorderColor: "#eeeeee"
    );

    public static readonly CvThemeConfig Modern = new(
        ThemeClass: "cv-modern",
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

    public static CvThemeConfig GetConfig(string template)
    {
        return template switch
        {
            AiCV.Domain.Constants.CvTemplates.Modern => Modern,
            AiCV.Domain.Constants.CvTemplates.Minimalist => Minimalist,
            _ => Professional,
        };
    }
}
