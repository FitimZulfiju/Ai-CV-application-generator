namespace AiCV.Domain;

public class User : IdentityUser
{
    public CandidateProfile? CandidateProfile { get; set; }
    public UserSettings? UserSettings { get; set; }
    public List<GeneratedApplication> GeneratedApplications { get; set; } = [];
    public List<Note> Notes { get; set; } = [];
    public List<UserAIConfiguration> UserAIConfigurations { get; set; } = [];
}
