namespace AiCV.Domain;

public class CandidateProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string LinkedInUrl { get; set; } = string.Empty;
    public string PortfolioUrl { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ProfessionalSummary { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public bool ShowProfilePicture { get; set; } = false;
    public string Tagline { get; set; } = string.Empty;
    public string FooterText { get; set; } = string.Empty;
    public List<Experience> WorkExperience { get; set; } = [];
    public List<Education> Educations { get; set; } = [];
    public List<Skill> Skills { get; set; } = [];
    public List<Project> Projects { get; set; } = [];
    public List<Language> Languages { get; set; } = [];
    public List<Interest> Interests { get; set; } = [];

    // Customizable Section Headers
    public SectionConfig SummarySection { get; set; } = new();
    public SectionConfig ExperienceSection { get; set; } = new();
    public SectionConfig EducationSection { get; set; } = new();
    public SectionConfig SkillsSection { get; set; } = new();
    public SectionConfig ProjectsSection { get; set; } = new();
    public SectionConfig LanguagesSection { get; set; } = new();
    public SectionConfig InterestsSection { get; set; } = new();
}

public class SectionConfig
{
    private string _title = string.Empty;
    public string Title 
    { 
        get => _title; 
        set => _title = value ?? string.Empty; 
    }

    private string _icon = string.Empty;
    public string Icon 
    { 
        get => _icon; 
        set => _icon = value ?? string.Empty; 
    }
}

public class Skill
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public CandidateProfile? CandidateProfile { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class Experience
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public CandidateProfile? CandidateProfile { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsCurrentRole { get; set; }
}

public class Education
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public CandidateProfile? CandidateProfile { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class Project
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public CandidateProfile? CandidateProfile { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // e.g. "Full-Stack Developer"
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;
    public string SectionDescription { get; set; } = string.Empty;
    public string Technologies { get; set; } = string.Empty; // Comma separated or formatted
    public string Link { get; set; } = string.Empty;
}

public class Language
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public CandidateProfile? CandidateProfile { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Proficiency { get; set; } = string.Empty; // e.g. "Native", "Fluent"
}

public class Interest
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public CandidateProfile? CandidateProfile { get; set; }
    public string Name { get; set; } = string.Empty;
}
