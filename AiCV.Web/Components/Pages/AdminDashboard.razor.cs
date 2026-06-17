namespace AiCV.Web.Components.Pages;

public partial class AdminDashboard
{
    private async Task ExportCsv()
    {
        try
        {
            var csvBytes = await StatisticsService.GetStatisticsCsvAsync();
            await using var stream = new MemoryStream(csvBytes);
            using var streamRef = new DotNetStreamReference(stream);

            await JSRuntime.InvokeVoidAsync(
                "downloadFileFromStream",
                $"AiCV_Export_{DateTime.UtcNow:yyyyMMdd}.csv",
                streamRef
            );
            Snackbar.Add(Localizer["StatisticsExportedSuccessfully"], Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"{Localizer["ExportFailed"]}: {ex.Message}", Severity.Error);
        }
    }

    private AdminStatisticsDto? _statistics;

    protected override async Task OnInitializedAsync()
    {
        await LoadStatistics();
    }

    private async Task LoadStatistics()
    {
        LoadingService.Show(Localizer["LoadingAdminDashboard"], 0);
        try
        {
            _statistics = await StatisticsService.GetStatisticsAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"{Localizer["ErrorLoadingAdminDashboard"]}: {ex.Message}", Severity.Error);
        }
        finally
        {
            LoadingService.Hide();
        }
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
        var confirmed = await DialogService.ShowMessageBoxAsync(
            Localizer["ConfirmAction"],
            lockout ? Localizer["AreYouSureWantToLockUser"] : Localizer["AreYouSureWantToUnlockUser"],
            yesText: Localizer["Yes"],
            cancelText: Localizer["Cancel"]
        );

        if (confirmed == true)
        {
            var result = await UserManagementService.ToggleUserLockoutAsync(userId, lockout);
            if (result)
            {

                await LoadStatistics(); // Refresh data
            }
            else
            {
                var messageKey = lockout ? "FailedToLockUserAccount" : "FailedToUnlockUserAccount";
                Snackbar.Add($"{Localizer[messageKey]}", Severity.Error);
            }
        }
    }
}
