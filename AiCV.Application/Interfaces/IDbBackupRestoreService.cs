namespace AiCV.Application.Interfaces;

public interface IDbBackupRestoreService
{
    Task<bool> BackupDatabaseAsync(string backupFilePath);
    Task<bool> RestoreDatabaseFromZip(Stream backupFile, string restorePath);
    string GetBackupDirectory();
}
