namespace AiCV.Infrastructure.Services;

public class BackupBackgroundService(
    IBackupService backupService,
    ILogger<BackupBackgroundService> logger,
    IHostEnvironment environment
) : BackgroundService
{
    private readonly IBackupService _backupService = backupService;
    private readonly ILogger<BackupBackgroundService> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation(
                "BackupBackgroundService is disabled in Development environment."
            );
            return;
        }

        _logger.LogInformation("BackupBackgroundService starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calculate time until next backup (e.g., at 02:00 AM)
                var now = DateTime.Now;
                var nextBackupTime = now.Date.AddDays(1).AddHours(2); // Tomorrow at 2 AM
                var delay = nextBackupTime - now;

                _logger.LogInformation(
                    "Next backup scheduled for {NextBackupTime} (in {DelayHours} hours)",
                    nextBackupTime,
                    Math.Round(delay.TotalHours, 2)
                );

                await Task.Delay(delay, stoppingToken);

                _logger.LogInformation("Triggering scheduled backup...");
                await _backupService.CreateAndUploadBackupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BackupBackgroundService loop");
                // Wait a bit before retrying if something went wrong
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }
}
