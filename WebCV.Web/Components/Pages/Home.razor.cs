namespace WebCV.Web.Components.Pages;

public partial class Home
{
    [Inject] public NavigationManager Navigation { get; set; } = default!;

    public string AppVersion { get; set; } = "1.0.0";

    protected override void OnInitialized()
    {
        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        if (version != null)
        {
            AppVersion = $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }
}
