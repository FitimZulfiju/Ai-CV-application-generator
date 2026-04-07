namespace AiCV.Web;

public static class AppVersionProvider
{
    public static string GetDisplayVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var buildMetadataIndex = informationalVersion.IndexOf('+');
            return buildMetadataIndex >= 0
                ? informationalVersion[..buildMetadataIndex]
                : informationalVersion;
        }

        var version = assembly.GetName().Version;
        return version == null ? "1.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
