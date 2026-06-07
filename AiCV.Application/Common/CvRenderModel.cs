namespace AiCV.Application.Common;

public sealed record CvRenderModel(
    CandidateProfile? Profile,
    IReadOnlyList<CvSkillGroup> SkillGroups,
    IReadOnlyList<CvWorkExperienceItem> WorkExperiences,
    IReadOnlyList<CvEducationItem> Educations,
    IReadOnlyList<CvProjectItem> Projects,
    IReadOnlyList<Language> Languages,
    IReadOnlyList<Interest> Interests
)
{
    public static CvRenderModel FromProfile(CandidateProfile? profile, IStringLocalizer localizer)
    {
        return new CvRenderModel(
            profile,
            BuildSkillGroups(profile),
            BuildWorkExperiences(profile, localizer),
            BuildEducations(profile, localizer),
            BuildProjects(profile, localizer),
            profile?.Languages ?? [],
            profile?.Interests ?? []
        );
    }

    private static IReadOnlyList<CvSkillGroup> BuildSkillGroups(CandidateProfile? profile)
    {
        return (profile?.Skills ?? [])
            .GroupBy(skill => string.IsNullOrWhiteSpace(skill.Category) ? "Other" : skill.Category)
            .Select(group => new CvSkillGroup(
                group.Key,
                group.ToList(),
                string.Join(", ", group.Select(skill => skill.Name).Distinct())
            ))
            .ToList();
    }

    private static IReadOnlyList<CvWorkExperienceItem> BuildWorkExperiences(
        CandidateProfile? profile,
        IStringLocalizer localizer
    )
    {
        return (profile?.WorkExperience ?? [])
            .OrderByDescending(experience => experience.StartDate)
            .Select(experience => new CvWorkExperienceItem(
                experience,
                FormatMonthRange(
                    experience.StartDate,
                    experience.EndDate,
                    experience.IsCurrentRole,
                    localizer
                ),
                CvHelpers.CalculateDuration(
                    experience.StartDate,
                    experience.EndDate,
                    experience.IsCurrentRole,
                    localizer
                ),
                FormatCompanyLine(experience.CompanyName, experience.Location)
            ))
            .ToList();
    }

    private static IReadOnlyList<CvEducationItem> BuildEducations(
        CandidateProfile? profile,
        IStringLocalizer localizer
    )
    {
        return (profile?.Educations ?? [])
            .OrderByDescending(education => education.StartDate)
            .Select(education => new CvEducationItem(
                education,
                FormatYearRange(education.StartDate, education.EndDate, localizer)
            ))
            .ToList();
    }

    private static IReadOnlyList<CvProjectItem> BuildProjects(
        CandidateProfile? profile,
        IStringLocalizer localizer
    )
    {
        return (profile?.Projects ?? [])
            .OrderByDescending(project => project.StartDate)
            .Select(project => new CvProjectItem(
                project,
                FormatYearRange(project.StartDate, project.EndDate, localizer),
                ParseProjectSection(project)
            ))
            .ToList();
    }

    public static string FormatMonthRange(
        DateTime? start,
        DateTime? end,
        bool isCurrentRole,
        IStringLocalizer localizer
    )
    {
        return $"{start?.ToString("MM/yyyy")} \u2013 {(isCurrentRole ? localizer["Present"] : (end.HasValue ? end.Value.ToString("MM/yyyy") : localizer["Present"]))}";
    }

    public static string FormatYearRange(
        DateTime? start,
        DateTime? end,
        IStringLocalizer localizer
    )
    {
        return $"{start?.ToString("yyyy")} - {(end.HasValue ? end.Value.ToString("yyyy") : localizer["Present"])}";
    }

    public static CvProjectSection ParseProjectSection(Project project)
    {
        if (
            string.IsNullOrWhiteSpace(project.SectionTitle)
            && string.IsNullOrWhiteSpace(project.SectionDescription)
        )
        {
            return CvProjectSection.Empty;
        }

        if (!string.IsNullOrWhiteSpace(project.SectionDescription))
        {
            return new CvProjectSection(
                project.SectionTitle?.Trim() ?? string.Empty,
                project.SectionDescription
            );
        }

        var sectionLines = (project.SectionTitle ?? string.Empty)
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (sectionLines.Length == 0)
        {
            return CvProjectSection.Empty;
        }

        return new CvProjectSection(
            sectionLines[0],
            sectionLines.Length > 1 ? string.Join("\n", sectionLines.Skip(1)) : string.Empty
        );
    }

    private static string FormatCompanyLine(string? companyName, string? location)
    {
        return (companyName ?? string.Empty)
            + (string.IsNullOrEmpty(location) ? string.Empty : $" - {location}");
    }
}

public sealed record CvSkillGroup(string Category, IReadOnlyList<Skill> Skills, string SkillNames);

public sealed record CvWorkExperienceItem(
    Experience Source,
    string DateRange,
    string Duration,
    string CompanyLine
)
{
    public string? JobTitle => Source.JobTitle;
    public string? CompanyName => Source.CompanyName;
    public string? Location => Source.Location;
    public string? Description => Source.Description;
}

public sealed record CvEducationItem(Education Source, string DateRange)
{
    public string? Degree => Source.Degree;
    public string? InstitutionName => Source.InstitutionName;
    public string? Description => Source.Description;
}

public sealed record CvProjectItem(Project Source, string DateRange, CvProjectSection Section)
{
    public string? Name => Source.Name;
    public string? Link => Source.Link;
    public string? Technologies => Source.Technologies;
    public string? Role => Source.Role;
    public string? Description => Source.Description;
}

public sealed record CvProjectSection(string Header, string Details)
{
    public static CvProjectSection Empty { get; } = new(string.Empty, string.Empty);
}
