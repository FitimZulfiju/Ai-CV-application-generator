namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class ProjectsTab
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    private void AddProject()
    {
        Profile?.Projects.Add(new Project { StartDate = DateTime.Now });
    }

    private void RemoveProject(Project proj)
    {
        Profile?.Projects.Remove(proj);
    }
}
