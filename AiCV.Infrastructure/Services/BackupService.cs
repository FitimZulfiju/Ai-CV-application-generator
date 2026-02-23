namespace AiCV.Infrastructure.Services;

public class BackupService(IServiceProvider serviceProvider, ILogger<BackupService> logger)
    : IBackupService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<BackupService> _logger = logger;

    public async Task<bool> CreateAndUploadBackupAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[CreateAndUploadBackupAsync] Starting backup process...");
        using var scope = _serviceProvider.CreateScope();
        var dbBackupRestoreService =
            scope.ServiceProvider.GetRequiredService<IDbBackupRestoreService>();

        try
        {
            string backupDir = dbBackupRestoreService.GetBackupDirectory();
            Directory.CreateDirectory(backupDir);

            string fileName = $"backup_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.zip";
            string backupFilePath = Path.Combine(backupDir, fileName);

            var stopwatch = Stopwatch.StartNew();
            bool backupSuccessful = await dbBackupRestoreService.BackupDatabaseAsync(
                backupFilePath
            );
            stopwatch.Stop();

            if (backupSuccessful)
            {
                _logger.LogInformation(
                    "[CreateAndUploadBackupAsync] Backup successful in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds
                );
                await RotateBackupsAsync(3); // Keep last 3 backups
            }
            else
            {
                _logger.LogWarning("[CreateAndUploadBackupAsync] Backup failed.");
            }

            return backupSuccessful;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CreateAndUploadBackupAsync] Error during backup process");
            return false;
        }
    }

    public async Task<bool> RotateBackupsAsync(int keepCount = 3)
    {
        _logger.LogInformation(
            "[RotateBackupsAsync] Starting backup rotation (keeping {KeepCount} latest)...",
            keepCount
        );
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbBackupRestoreService =
                scope.ServiceProvider.GetRequiredService<IDbBackupRestoreService>();
            string backupDir = dbBackupRestoreService.GetBackupDirectory();

            if (!Directory.Exists(backupDir))
                return true;

            var directoryInfo = new DirectoryInfo(backupDir);
            var backupFiles = directoryInfo
                .GetFiles("backup_*.zip")
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            if (backupFiles.Count <= keepCount)
            {
                _logger.LogInformation(
                    "[RotateBackupsAsync] Found {Count} backups, which is <= {KeepCount}. No cleanup needed.",
                    backupFiles.Count,
                    keepCount
                );
                return true;
            }

            var filesToDelete = backupFiles.Skip(keepCount).ToList();
            foreach (var file in filesToDelete)
            {
                try
                {
                    _logger.LogInformation(
                        "[RotateBackupsAsync] Deleting old backup: {FileName}",
                        file.Name
                    );
                    file.Delete();
                }
                catch (Exception deleteEx)
                {
                    _logger.LogWarning(
                        deleteEx,
                        "[RotateBackupsAsync] Failed to delete old backup file: {FileName}",
                        file.Name
                    );
                }
            }

            _logger.LogInformation(
                "[RotateBackupsAsync] Backup rotation completed. Deleted {DeletedCount} files.",
                filesToDelete.Count
            );
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RotateBackupsAsync] Error during backup rotation");
            return false;
        }
    }
}
