namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class LanguagesTab
{
    [Parameter]
    public CandidateProfile? Profile { get; set; }

    private string _newLanguageName = string.Empty;
    private string _newLanguageProficiency = string.Empty;
    private Language? _editingLanguage;
    private bool _showChipHelp = true;

    private void SaveLanguage()
    {
        if (Profile == null || string.IsNullOrWhiteSpace(_newLanguageName))
        {
            return;
        }

        var languageName = _newLanguageName.Trim();
        var proficiency = _newLanguageProficiency.Trim();

        if (Profile.Languages.Any(l =>
                !ReferenceEquals(l, _editingLanguage)
                && string.Equals(l.Name, languageName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(l.Proficiency ?? string.Empty, proficiency, StringComparison.OrdinalIgnoreCase)))
        {
            ClearLanguageEditor();
            return;
        }

        if (_editingLanguage == null)
        {
            Profile.Languages.Add(
                new Language
                {
                    Name = languageName,
                    Proficiency = proficiency,
                }
            );
        }
        else
        {
            _editingLanguage.Name = languageName;
            _editingLanguage.Proficiency = proficiency;
        }

        ClearLanguageEditor();
    }

    private void BeginLanguageEdit(Language language)
    {
        _editingLanguage = language;
        _newLanguageName = language.Name;
        _newLanguageProficiency = language.Proficiency;
    }

    private void CancelLanguageEdit()
    {
        ClearLanguageEditor();
    }

    private void ClearLanguageEditor()
    {
        _editingLanguage = null;
        _newLanguageName = string.Empty;
        _newLanguageProficiency = string.Empty;
    }

    private static string GetLanguageChipText(Language language)
    {
        var languageName = language.Name?.Trim() ?? string.Empty;
        var proficiency = language.Proficiency?.Trim() ?? string.Empty;

        return string.IsNullOrWhiteSpace(proficiency)
            ? languageName
            : $"{languageName} {proficiency}";
    }

    private Color GetLanguageChipColor(Language language)
    {
        return ReferenceEquals(language, _editingLanguage) ? Color.Secondary : Color.Primary;
    }

    private Variant GetLanguageChipVariant(Language language)
    {
        return ReferenceEquals(language, _editingLanguage) ? Variant.Filled : Variant.Outlined;
    }

    private void MoveLanguageLeft(Language lang)
    {
        if (Profile == null) return;
        var idx = Profile.Languages.IndexOf(lang);
        if (idx > 0)
        {
            Profile.Languages.RemoveAt(idx);
            Profile.Languages.Insert(idx - 1, lang);
        }
    }

    private void MoveLanguageRight(Language lang)
    {
        if (Profile == null) return;
        var idx = Profile.Languages.IndexOf(lang);
        if (idx >= 0 && idx < Profile.Languages.Count - 1)
        {
            Profile.Languages.RemoveAt(idx);
            Profile.Languages.Insert(idx + 1, lang);
        }
    }

    private void RemoveLanguage(Language lang)
    {
        if (Profile == null)
        {
            return;
        }

        if (ReferenceEquals(lang, _editingLanguage))
        {
            ClearLanguageEditor();
        }

        Profile.Languages.Remove(lang);
    }
}
