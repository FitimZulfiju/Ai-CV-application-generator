namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class ExperienceTab
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    private void AddExperience()
    {
        Profile?.WorkExperience.Add(new Experience { StartDate = DateTime.Now });
    }

    private void RemoveExperience(Experience exp)
    {
        Profile?.WorkExperience.Remove(exp);
    }

    private string CalculateDuration(DateTime? start, DateTime? end, bool isCurrentRole = false)
    {
        return CvHelpers.CalculateDuration(start, end, isCurrentRole, Localizer);
    }
}
