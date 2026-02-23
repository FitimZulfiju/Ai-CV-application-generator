namespace AiCV.Infrastructure.Services;

public class DbBackupRestoreService(
    IServiceScopeFactory scopeFactory,
    ILogger<DbBackupRestoreService> logger,
    IConfiguration configuration,
    IWebHostEnvironment environment
) : IDbBackupRestoreService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<DbBackupRestoreService> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly IWebHostEnvironment _environment = environment;

    public async Task<bool> BackupDatabaseAsync(string backupFilePath)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var dbConn = context.Database.GetDbConnection();

            // Check if we're using SQL Server
            if (dbConn is not SqlConnection sqlConn)
            {
                _logger.LogWarning(
                    "Backup only supported for SQL Server. Current provider: {Provider}",
                    dbConn.GetType().Name
                );
                return false;
            }

            string dbName = sqlConn.Database;
            string backupDir = Path.GetDirectoryName(backupFilePath)!;

            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            // Create a temporary .bak file
            string tempBakFile = Path.Combine(backupDir, $"backup_{Guid.NewGuid()}.bak");
            string sql = $"BACKUP DATABASE [{dbName}] TO DISK = @backupFile WITH FORMAT, INIT;";

            await using var cmd = new SqlCommand(sql, sqlConn);
            cmd.Parameters.AddWithValue("@backupFile", tempBakFile);

            if (sqlConn.State != ConnectionState.Open)
                await sqlConn.OpenAsync();

            await cmd.ExecuteNonQueryAsync();

            // Create zip file containing the .bak file and assets
            await using (var zipStream = new FileStream(backupFilePath, FileMode.Create))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                // Add the database backup file
                string entryName = $"database_backup_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.bak";
                var dbEntry = archive.CreateEntry(entryName);
                await using (var entryStream = dbEntry.Open())
                await using (
                    var bakStream = new FileStream(tempBakFile, FileMode.Open, FileAccess.Read)
                )
                {
                    await bakStream.CopyToAsync(entryStream);
                }

                // Add media directories
                await AddDirectoryToArchiveAsync(archive, "uploads");
                await AddDirectoryToArchiveAsync(archive, "images");
            }

            // Clean up temporary .bak file
            if (File.Exists(tempBakFile))
            {
                File.Delete(tempBakFile);
            }

            _logger.LogInformation("Backup created successfully: {Path}", backupFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during backup process");
            return false;
        }
    }

    private async Task AddDirectoryToArchiveAsync(ZipArchive archive, string subDir)
    {
        string dirPath = Path.Combine(_environment.WebRootPath, subDir);
        if (!Directory.Exists(dirPath))
            return;

        foreach (var file in Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(_environment.WebRootPath, file);
            var entry = archive.CreateEntry(relativePath.Replace("\\", "/"));
            await using var entryStream = entry.Open();
            await using var fileStream = new FileStream(
                file,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            );
            await fileStream.CopyToAsync(entryStream);
        }
    }

    public string GetBackupDirectory()
    {
        // Check for environment variable
        var envPath = _configuration["BACKUP_DIR"] ?? _configuration["BACKUP_PATH"];
        if (!string.IsNullOrWhiteSpace(envPath))
            return envPath;

        // Default to a folder in the app base directory
        return Path.Combine(AppContext.BaseDirectory, "Backups");
    }

    public async Task<bool> RestoreDatabaseFromZip(Stream backupFile, string restorePath)
    {
        // Partial implementation as seen in MFMud8, focusing on recovery first
        _logger.LogWarning("RestoreDatabaseFromZip not fully implemented for AiCV yet.");
        return await Task.FromResult(false);
    }
}
