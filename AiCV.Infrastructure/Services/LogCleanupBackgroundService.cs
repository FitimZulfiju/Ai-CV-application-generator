namespace AiCV.Infrastructure.Services;

public class LogCleanupBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<LogCleanupBackgroundService> logger) : BackgroundService
{
    private readonly int _daysToKeep = 30;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("LogCleanupBackgroundService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Running automated system log cleanup...");

                using (var scope = serviceProvider.CreateScope())
                {
                    var logService = scope.ServiceProvider.GetRequiredService<ISystemLogService>();
                    
                    // Clear logs older than the retention period
                    await logService.ClearLogsAsync(_daysToKeep);
                }

                logger.LogInformation("System log cleanup completed successfully. Removed logs older than {Days} days.", _daysToKeep);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing automated system log cleanup.");
            }

            // Wait until the next check interval before running again
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}
