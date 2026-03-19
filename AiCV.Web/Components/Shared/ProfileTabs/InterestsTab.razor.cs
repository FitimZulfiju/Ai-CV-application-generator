namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class InterestsTab
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    private void AddInterest()
    {
        Profile?.Interests.Add(new Interest());
    }

    private void RemoveInterest(Interest interest)
    {
        Profile?.Interests.Remove(interest);
    }
}
