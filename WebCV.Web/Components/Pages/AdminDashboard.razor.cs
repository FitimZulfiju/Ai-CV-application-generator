namespace WebCV.Web.Components.Pages;

public partial class AdminDashboard
{
    [Inject]
    public IAdminStatisticsService StatisticsService { get; set; } = default!;

    [Inject]
    public IUserManagementService UserManagementService { get; set; } = default!;

    [Inject]
    public IDialogService DialogService { get; set; } = default!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    public IJSRuntime JS { get; set; } = default!;

    private async Task ExportCsv()
    {
        try
        {
            var csvBytes = await StatisticsService.GetStatisticsCsvAsync();
            await using var stream = new MemoryStream(csvBytes);
            using var streamRef = new DotNetStreamReference(stream);

            await JS.InvokeVoidAsync(
                "downloadFileFromStream",
                $"WebCV_Export_{DateTime.UtcNow:yyyyMMdd}.csv",
                streamRef
            );
            Snackbar.Add("Statistics exported successfully", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
        }
    }

    private AdminStatisticsDto? _statistics;
    private bool _loading = true;

    // Chart data
    private List<ChartSeries> _dailyChartSeries = [];
    private string[] _dailyLabels = [];
    private readonly ChartOptions _chartOptions = new() { YAxisTicks = 1 };

    protected override async Task OnInitializedAsync()
    {
        await LoadStatistics();
    }

    private async Task LoadStatistics()
    {
        _loading = true;
        try
        {
            _statistics = await StatisticsService.GetStatisticsAsync();
            PrepareChartData();
        }
        finally
        {
            _loading = false;
        }
    }

    private void PrepareChartData()
    {
        if (_statistics == null)
            return;

        // Prepare daily chart data
        double[] dailyData = [.. _statistics.DailyApplicationCounts.Select(d => (double)d.Count)];

        _dailyLabels =
        [
            .. _statistics.DailyApplicationCounts.Select(d => d.Date.ToString("MM/dd")),
        ];

        _dailyChartSeries = [new ChartSeries { Name = "Applications", Data = dailyData }];
    }

    private static Color GetRankColor(int index) =>
        index switch
        {
            0 => Color.Warning, // Gold
            1 => Color.Default, // Silver
            2 => Color.Tertiary, // Bronze
            _ => Color.Primary,
        };

    private async Task ToggleUserLockout(string userId, bool lockout)
    {
        var action = lockout ? "lock" : "unlock";
        var confirmed = await DialogService.ShowMessageBox(
            "Confirm Action",
            $"Are you sure you want to {action} this user account?",
            yesText: "Yes",
            cancelText: "Cancel"
        );

        if (confirmed == true)
        {
            var result = await UserManagementService.ToggleUserLockoutAsync(userId, lockout);
            if (result)
            {
                Snackbar.Add($"User account {action}ed successfully", Severity.Success);
                await LoadStatistics(); // Refresh data
            }
            else
            {
                Snackbar.Add($"Failed to {action} user account", Severity.Error);
            }
        }
    }
}
