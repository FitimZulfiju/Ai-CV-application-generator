namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class InterestsTab
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    private string _newInterest = string.Empty;

    private void AddInterest()
    {
        if (Profile == null || string.IsNullOrWhiteSpace(_newInterest))
        {
            return;
        }

        var interestName = _newInterest.Trim();
        if (Profile.Interests.Any(i => string.Equals(i.Name, interestName, StringComparison.OrdinalIgnoreCase)))
        {
            _newInterest = string.Empty;
            return;
        }

        Profile.Interests.Add(new Interest { Name = interestName });
        _newInterest = string.Empty;
    }

    private void RemoveInterest(Interest interest)
    {
        Profile?.Interests.Remove(interest);
    }
}
