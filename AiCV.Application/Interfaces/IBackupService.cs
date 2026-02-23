namespace AiCV.Application.Interfaces;

public interface IBackupService
{
    Task<bool> CreateAndUploadBackupAsync(CancellationToken stoppingToken);
    Task<bool> RotateBackupsAsync(int keepCount = 3);
}
