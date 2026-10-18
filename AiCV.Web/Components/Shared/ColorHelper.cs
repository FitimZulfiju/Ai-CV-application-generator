namespace AiCV.Web.Components.Shared;

public static class ColorHelper
{
    private static readonly Color[] ProviderPalette =
    [
        Color.Primary,
        Color.Info,
        Color.Success,
        Color.Warning,
        Color.Secondary,
        Color.Tertiary,
        Color.Dark,
        Color.Error
    ];

    public static Color GetProviderColor(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
            return Color.Default;

        var normalized = provider.Trim();

        // Check well-known job search providers for recognizable brand-aligned colors
        if (normalized.Contains("jobindex", StringComparison.OrdinalIgnoreCase))
            return Color.Primary;
        if (normalized.Contains("linkedin", StringComparison.OrdinalIgnoreCase))
            return Color.Info;
        if (normalized.Contains("indeed", StringComparison.OrdinalIgnoreCase))
            return Color.Tertiary;
        if (normalized.Contains("glassdoor", StringComparison.OrdinalIgnoreCase))
            return Color.Success;
        if (normalized.Contains("jsearch", StringComparison.OrdinalIgnoreCase))
            return Color.Warning;
        if (normalized.Contains("google", StringComparison.OrdinalIgnoreCase))
            return Color.Secondary;

        // Check if an AIProvider enum name was passed as a string
        if (Enum.TryParse<AIProvider>(normalized, true, out var aiProvider))
            return GetProviderColor(aiProvider);

        // Deterministic hash so any distinct provider gets a consistent distinct color from the palette
        var hash = 0;
        foreach (var ch in normalized.ToLowerInvariant())
        {
            hash = (hash * 31) + ch;
        }

        return ProviderPalette[Math.Abs(hash) % ProviderPalette.Length];
    }

    public static Color GetProviderColor(AIProvider provider) =>
        provider switch
        {
            AIProvider.GoogleGemini => Color.Primary,
            AIProvider.OpenAI => Color.Success,
            AIProvider.Claude => Color.Warning,
            AIProvider.Groq => Color.Info,
            AIProvider.DeepSeek => Color.Secondary,
            AIProvider.OpenRouter => Color.Dark,
            AIProvider.OmniRouter => Color.Dark,
            AIProvider.Custom => Color.Surface,
            _ => Color.Default,
        };

    public static string GetProviderIcon(AIProvider provider) =>
        provider switch
        {
            AIProvider.GoogleGemini => Icons.Material.Filled.AutoAwesome,
            AIProvider.OpenAI => Icons.Material.Filled.Psychology,
            AIProvider.Claude => Icons.Material.Filled.SmartToy,
            AIProvider.Groq => Icons.Material.Filled.Speed,
            AIProvider.DeepSeek => Icons.Material.Filled.Explore,
            AIProvider.OpenRouter => Icons.Material.Filled.Hub,
            AIProvider.OmniRouter => Icons.Material.Filled.Hub,
            AIProvider.Custom => Icons.Material.Filled.Build,
            _ => Icons.Material.Filled.Memory,
        };

    public static Color GetStatusColor(string? status) =>
        status switch
        {
            ApplicationStatus.PendingReview => Color.Warning,
            ApplicationStatus.Approved => Color.Info,
            ApplicationStatus.AutoApplied => Color.Success,
            ApplicationStatus.Applied => Color.Success,
            ApplicationStatus.Rejected => Color.Error,
            ApplicationStatus.Archived => Color.Default,
            _ => Color.Default,
        };
}

