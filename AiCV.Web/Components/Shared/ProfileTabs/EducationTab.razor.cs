namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class EducationTab
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    private void AddEducation()
    {
        Profile?.Educations.Add(new Education { StartDate = DateTime.Now });
    }

    private void RemoveEducation(Education edu)
    {
        Profile?.Educations.Remove(edu);
    }
}
