namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class ExperienceTab
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    private void AddExperience()
    {
        if (Profile == null) return;
        Profile.WorkExperience.Add(new Experience { StartDate = DateTime.Now });
    }

    private void RemoveExperience(Experience exp)
    {
        if (Profile == null) return;
        Profile.WorkExperience.Remove(exp);
    }

    private void MoveExperienceUp(Experience exp)
    {
        if (Profile == null) return;
        var index = Profile.WorkExperience.IndexOf(exp);
        if (index > 0)
        {
            Profile.WorkExperience.RemoveAt(index);
            Profile.WorkExperience.Insert(index - 1, exp);
        }
    }

    private void MoveExperienceDown(Experience exp)
    {
        if (Profile == null) return;
        var index = Profile.WorkExperience.IndexOf(exp);
        if (index >= 0 && index < Profile.WorkExperience.Count - 1)
        {
            Profile.WorkExperience.RemoveAt(index);
            Profile.WorkExperience.Insert(index + 1, exp);
        }
    }

    private string CalculateDuration(DateTime? start, DateTime? end, bool isCurrentRole = false)
    {
        return CvHelpers.CalculateDuration(start, end, isCurrentRole, Localizer);
    }
}
