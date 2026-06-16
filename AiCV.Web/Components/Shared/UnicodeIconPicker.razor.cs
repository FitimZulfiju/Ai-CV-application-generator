namespace AiCV.Web.Components.Shared;

public partial class UnicodeIconPicker
{
    [Parameter]
    public string? Label { get; set; }

    protected override void OnParametersSet()
    {
        Label ??= Localizer["SelectIcon"].Value;
    }

    [Parameter]
    public string Icon { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> IconChanged { get; set; }

    private async Task OnIconChanged(string value)
    {
        Icon = value;
        await IconChanged.InvokeAsync(value);
    }

    private static readonly Dictionary<string, string> _iconDictionary = new(StringComparer.OrdinalIgnoreCase)
    {
        { "User / Profile", "👤" },
        { "Tie / Professional", "👔" },
        { "Briefcase / Experience", "💼" },
        { "Building / Company", "🏢" },
        { "Chart / Growth", "📈" },
        { "Tools / Technical", "🛠️" },
        { "Hourglass / Time", "⏳" },
        { "Graduation Cap / Education", "🎓" },
        { "School / University", "🏫" },
        { "Books / Study", "📚" },
        { "Certificate / Diploma", "📜" },
        { "Star / Skills", "⭐" },
        { "Lightning / Quick", "⚡" },
        { "Target / Goals", "🎯" },
        { "Lightbulb / Idea", "💡" },
        { "Brain / Knowledge", "🧠" },
        { "Wrench / Fixing", "🔧" },
        { "Laptop / Coding", "💻" },
        { "Rocket / Launch", "🚀" },
        { "Folder / Projects", "📁" },
        { "Bar Chart / Stats", "📊" },
        { "Mobile / App", "📱" },
        { "Globe / Languages", "🌐" },
        { "Speaking / Fluent", "🗣️" },
        { "Earth / International", "🌍" },
        { "Chat / Communication", "💬" },
        { "Palette / Art", "🎨" },
        { "Music / Audio", "🎵" },
        { "Football / Sports", "⚽" },
        { "Airplane / Travel", "✈️" },
        { "Camera / Photography", "📸" },
        { "Game Controller", "🎮" },
        { "Diamond / Value", "💎" },
        { "Pin / Location", "📌" },
        { "Checkmark", "✅" },
        { "Fire / Passion", "🔥" },
        { "Heart", "❤️" },
        { "Shield / Security", "🛡️" },
        { "Cloud", "☁️" },
        { "Search / Find", "🔍" },
        { "Settings / Gears", "⚙️" }
    };

    private static Task<IEnumerable<string>> SearchIcons(string value, CancellationToken token)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Task.FromResult(_iconDictionary.Values.AsEnumerable());
        }

        // Search by name or if the user pasted the icon directly
        var matches = _iconDictionary
            .Where(kvp => kvp.Key.Contains(value, StringComparison.OrdinalIgnoreCase) || kvp.Value == value)
            .Select(kvp => kvp.Value);

        // If no match but user pasted an emoji, allow it
        if (!matches.Any() && !string.IsNullOrWhiteSpace(value) && value.Length <= 4)
        {
            return Task.FromResult(new[] { value }.AsEnumerable());
        }

        return Task.FromResult(matches);
    }

    private string GetIconName(string iconValue)
    {
        var match = _iconDictionary.FirstOrDefault(x => x.Value == iconValue);
        return match.Key ?? Localizer["CustomIcon"].Value;
    }
}
