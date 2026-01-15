namespace AiCV.Web.Components.Shared;

public partial class LanguageSwitcher
{
    private static string CurrentLanguageCode =>
        CultureInfo.CurrentCulture.Name switch
        {
            "sq" or "sq-AL" => "AL",
            "da" or "da-DK" => "DK",
            _ => "EN",
        };

    private void SwitchLanguage(string culture)
    {
        var uri = new Uri(Navigation.Uri);
        var redirectUri = Uri.EscapeDataString(uri.ToString());
        Navigation.NavigateTo(
            $"/culture/set?culture={culture}&redirectUri={redirectUri}",
            forceLoad: true
        );
    }
}
