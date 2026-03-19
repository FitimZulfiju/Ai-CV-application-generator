namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class LanguagesTab
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    private void AddLanguage()
    {
        Profile?.Languages.Add(new Language());
    }

    private void RemoveLanguage(Language lang)
    {
        Profile?.Languages.Remove(lang);
    }
}
