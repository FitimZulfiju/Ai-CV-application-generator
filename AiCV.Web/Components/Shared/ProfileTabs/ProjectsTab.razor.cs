namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class ProjectsTab
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    private void AddProject()
    {
        if (Profile == null) return;
        Profile.Projects.Add(new Project { StartDate = DateTime.Now });
    }

    private void RemoveProject(Project proj)
    {
        if (Profile == null) return;
        Profile.Projects.Remove(proj);
    }

    private void MoveProjectUp(Project proj)
    {
        if (Profile == null) return;
        var index = Profile.Projects.IndexOf(proj);
        if (index > 0)
        {
            Profile.Projects.RemoveAt(index);
            Profile.Projects.Insert(index - 1, proj);
        }
    }

    private void MoveProjectDown(Project proj)
    {
        if (Profile == null) return;
        var index = Profile.Projects.IndexOf(proj);
        if (index >= 0 && index < Profile.Projects.Count - 1)
        {
            Profile.Projects.RemoveAt(index);
            Profile.Projects.Insert(index + 1, proj);
        }
    }
}
