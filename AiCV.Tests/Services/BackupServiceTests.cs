namespace AiCV.Tests.Services;

public class BackupServiceTests : IDisposable
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IDbBackupRestoreService> _dbBackupMock;
    private readonly Mock<ILogger<BackupService>> _loggerMock;
    private readonly BackupService _service;
    private readonly string _testBackupDir;

    public BackupServiceTests()
    {
        _testBackupDir = Path.Combine(Path.GetTempPath(), "AiCV_TestBackups_" + Guid.NewGuid());
        Directory.CreateDirectory(_testBackupDir);

        _serviceProviderMock = new Mock<IServiceProvider>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _dbBackupMock = new Mock<IDbBackupRestoreService>();
        _loggerMock = new Mock<ILogger<BackupService>>();

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_scopeFactoryMock.Object);

        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);

        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IDbBackupRestoreService)))
            .Returns(_dbBackupMock.Object);

        _dbBackupMock.Setup(x => x.GetBackupDirectory()).Returns(_testBackupDir);

        _service = new BackupService(_serviceProviderMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAndUploadBackupAsync_Success_ReturnsTrue_AndRotates()
    {
        // Arrange
        _dbBackupMock
            .Setup(x => x.BackupDatabaseAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Create some fake backups to trigger rotation
        for (int i = 0; i < 5; i++)
        {
            File.WriteAllText(Path.Combine(_testBackupDir, $"backup_test_{i}.zip"), "dummy");
            await Task.Delay(10, TestContext.Current.CancellationToken); // Ensure different creation times
        }

        // Act
        var result = await _service.CreateAndUploadBackupAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
        _dbBackupMock.Verify(x => x.BackupDatabaseAsync(It.Is<string>(s => s.Contains("backup_") && s.EndsWith(".zip"))), Times.Once);
        
        // Assert rotation happened (keep 3 + 1 new = 4, but since our fake ones don't match the exact pattern maybe they aren't deleted? 
        // Wait, RotateBackups looks for "backup_*.zip". 
        var remainingFiles = Directory.GetFiles(_testBackupDir, "backup_*.zip");
        Assert.True(remainingFiles.Length <= 3, $"Expected <= 3 files, found {remainingFiles.Length}");
    }

    [Fact]
    public async Task CreateAndUploadBackupAsync_Failure_ReturnsFalse()
    {
        // Arrange
        _dbBackupMock
            .Setup(x => x.BackupDatabaseAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.CreateAndUploadBackupAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
        _dbBackupMock.Verify(x => x.BackupDatabaseAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RotateBackupsAsync_DeletesOldestFiles()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            File.WriteAllText(Path.Combine(_testBackupDir, $"backup_{i}.zip"), "dummy");
            await Task.Delay(100, TestContext.Current.CancellationToken); // Ensure different timestamps
        }

        // Act
        var result = await _service.RotateBackupsAsync(2);

        // Assert
        Assert.True(result);
        var remaining = Directory.GetFiles(_testBackupDir, "backup_*.zip");
        Assert.Equal(2, remaining.Length); // Kept 2
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBackupDir))
        {
            try
            {
                Directory.Delete(_testBackupDir, true);
            }
            catch { }
        }
    }
}
