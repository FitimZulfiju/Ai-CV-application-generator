namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class InterestsTab
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    private string _newInterest = string.Empty;
    private Interest? _editingInterest;
    private bool _showChipHelp = true;

    private void SaveInterest()
    {
        if (Profile == null || string.IsNullOrWhiteSpace(_newInterest))
        {
            return;
        }

        var interestName = _newInterest.Trim();
        if (Profile.Interests.Any(i =>
                !ReferenceEquals(i, _editingInterest)
                && string.Equals(i.Name, interestName, StringComparison.OrdinalIgnoreCase)))
        {
            ClearInterestEditor();
            return;
        }

        if (_editingInterest == null)
        {
            Profile.Interests.Add(new Interest { Name = interestName });
        }
        else
        {
            _editingInterest.Name = interestName;
        }

        ClearInterestEditor();
    }

    private void BeginInterestEdit(Interest interest)
    {
        _editingInterest = interest;
        _newInterest = interest.Name;
    }

    private void CancelInterestEdit()
    {
        ClearInterestEditor();
    }

    private void ClearInterestEditor()
    {
        _editingInterest = null;
        _newInterest = string.Empty;
    }

    private Color GetInterestChipColor(Interest interest)
    {
        return ReferenceEquals(interest, _editingInterest) ? Color.Secondary : Color.Primary;
    }

    private Variant GetInterestChipVariant(Interest interest)
    {
        return ReferenceEquals(interest, _editingInterest) ? Variant.Filled : Variant.Outlined;
    }

    private void MoveInterestLeft(Interest interest)
    {
        if (Profile == null) return;
        var idx = Profile.Interests.IndexOf(interest);
        if (idx > 0)
        {
            Profile.Interests.RemoveAt(idx);
            Profile.Interests.Insert(idx - 1, interest);
        }
    }

    private void MoveInterestRight(Interest interest)
    {
        if (Profile == null) return;
        var idx = Profile.Interests.IndexOf(interest);
        if (idx >= 0 && idx < Profile.Interests.Count - 1)
        {
            Profile.Interests.RemoveAt(idx);
            Profile.Interests.Insert(idx + 1, interest);
        }
    }

    private void RemoveInterest(Interest interest)
    {
        if (Profile == null)
        {
            return;
        }

        if (ReferenceEquals(interest, _editingInterest))
        {
            ClearInterestEditor();
        }

        Profile.Interests.Remove(interest);
    }
}
