namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class LanguagesTab
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    private string _newLanguageName = string.Empty;
    private string _newLanguageProficiency = string.Empty;

    private void AddLanguage()
    {
        if (Profile == null || string.IsNullOrWhiteSpace(_newLanguageName))
        {
            return;
        }

        var languageName = _newLanguageName.Trim();
        var proficiency = _newLanguageProficiency.Trim();

        if (Profile.Languages.Any(l =>
                string.Equals(l.Name, languageName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(l.Proficiency ?? string.Empty, proficiency, StringComparison.OrdinalIgnoreCase)))
        {
            _newLanguageName = string.Empty;
            _newLanguageProficiency = string.Empty;
            return;
        }

        Profile.Languages.Add(
            new Language
            {
                Name = languageName,
                Proficiency = proficiency,
            }
        );

        _newLanguageName = string.Empty;
        _newLanguageProficiency = string.Empty;
    }

    private void RemoveLanguage(Language lang)
    {
        Profile?.Languages.Remove(lang);
    }
}
