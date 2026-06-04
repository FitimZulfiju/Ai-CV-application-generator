namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class InterestsTab
{
    private const string InterestsDropZone = "interests";

    [Parameter]
    public CandidateProfile? Profile { get; set; }

    private string _newInterest = string.Empty;
    private Interest? _editingInterest;
    private MudDropContainer<Interest>? _dropContainer;
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
        _dropContainer?.Refresh();
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

    private void OnInterestDropped(MudItemDropInfo<Interest> dropInfo)
    {
        if (Profile == null || dropInfo.Item is null)
        {
            return;
        }

        var interest = dropInfo.Item;
        Profile.Interests.Remove(interest);

        var newIndex = Math.Min(dropInfo.IndexInZone, Profile.Interests.Count);
        Profile.Interests.Insert(newIndex, interest);
        _dropContainer?.Refresh();
    }

    private Color GetInterestChipColor(Interest interest)
    {
        return ReferenceEquals(interest, _editingInterest) ? Color.Secondary : Color.Primary;
    }

    private Variant GetInterestChipVariant(Interest interest)
    {
        return ReferenceEquals(interest, _editingInterest) ? Variant.Filled : Variant.Outlined;
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
        _dropContainer?.Refresh();
    }
}
