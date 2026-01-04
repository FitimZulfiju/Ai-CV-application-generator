namespace WebCV.Web.Components.Pages;

public partial class AdminLogs
{
    private MudTable<SystemLog> _table = default!;
    private string? _filterLevel;

    private async Task<TableData<SystemLog>> ServerReload(TableState state, CancellationToken token)
    {
        var logs = await LogService.GetLogsAsync(state.Page + 1, state.PageSize, _filterLevel);
        var total = await LogService.GetTotalLogsCountAsync(_filterLevel);
        return new TableData<SystemLog> { TotalItems = total, Items = logs };
    }

    private void OnFilterChanged() => _table.ReloadServerData();

    private async Task RefreshLogs() => await _table.ReloadServerData();

    private async Task ClearLogs()
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Clear Logs",
            "Delete logs older than 30 days?",
            yesText: "Yes",
            cancelText: "Cancel"
        );
        if (confirmed == true)
        {
            await LogService.ClearLogsAsync(30);
            await RefreshLogs();
        }
    }

    private async Task ShowDetails(SystemLog log)
    {
        var parameters = new DialogParameters { ["Log"] = log };
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true,
        };
        await DialogService.ShowAsync<LogDetailsDialog>("Log Details", parameters, options);
    }

    private Color GetLevelColor(string level) =>
        level switch
        {
            "Error" => Color.Error,
            "Warning" => Color.Warning,
            "Info" => Color.Info,
            _ => Color.Default,
        };
}
