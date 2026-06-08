namespace AiCV.Application.Common.Models;

public record CvThemeConfig(
    string ThemeClass,
    string PrimaryColor = "#2c7be5",
    string PrimaryDark = "#1e5fae",
    string AccentColor = "#10b981",
    string TextDark = "#1f2937",
    string TextMedium = "#4b5563",
    string BackgroundLight = "#f9fafb",
    string BorderColor = "#e5e7eb",
    bool UseSectionSeparators = false,
    bool CenterLanguageContent = false,
    bool UseInterestChips = false,
    bool UseReferencesFooterPanel = false,
    string? AdditionalSectionBorderColor = null,
    string? SummaryBorderColor = null,
    string? SkillsBorderColor = null,
    string? WorkCompanyColor = null,
    bool SuppressWorkDescriptionBullet = false,
    string? EducationBorderColor = null,
    string? CoverLetterBorderColor = null
)
{
    public string EffectiveAdditionalSectionBorderColor => AdditionalSectionBorderColor ?? PrimaryColor;
    public string EffectiveSummaryBorderColor => SummaryBorderColor ?? PrimaryColor;
    public string EffectiveSkillsBorderColor => SkillsBorderColor ?? PrimaryColor;
    public string EffectiveWorkCompanyColor => WorkCompanyColor ?? PrimaryColor;
    public string EffectiveEducationBorderColor => EducationBorderColor ?? AccentColor;
    public string EffectiveCoverLetterBorderColor => CoverLetterBorderColor ?? PrimaryColor;
}
