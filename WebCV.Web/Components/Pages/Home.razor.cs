namespace WebCV.Web.Components.Pages;

public partial class Home
{
    [Inject]
    public NavigationManager Navigation { get; set; } = default!;

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    private async Task OpenSampleCvDialog()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
        };

        await DialogService.ShowAsync<SampleCvDialog>("Sample CV", options);
    }

    private async Task OpenUserGuideDialog()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
        };

        await DialogService.ShowAsync<UserGuideDialog>("How It Works", options);
    }

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
