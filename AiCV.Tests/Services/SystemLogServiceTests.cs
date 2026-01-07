using Microsoft.EntityFrameworkCore;
using AiCV.Domain.Entities;
using AiCV.Infrastructure.Data;
using AiCV.Infrastructure.Services;

namespace AiCV.Tests.Services;

public class SystemLogServiceTests
{
    private readonly ApplicationDbContext _context;
    private readonly SystemLogService _service;

    public SystemLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new SystemLogService(_context);
    }

    [Fact]
    public async Task LogErrorAsync_ShouldAddLogToDatabase()
    {
        // Act
        await _service.LogErrorAsync(
            "Test Error",
            "StackTrace",
            "TestSource",
            "/test/path",
            "user123"
        );

        // Assert
        var log = await _context.SystemLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("Error", log.Level);
        Assert.Equal("Test Error", log.Message);
        Assert.Equal("StackTrace", log.StackTrace);
        Assert.Equal("TestSource", log.Source);
        Assert.Equal("/test/path", log.RequestPath);
        Assert.Equal("user123", log.UserId);
        Assert.True(log.Timestamp > DateTime.MinValue);
    }

    [Fact]
    public async Task GetLogsAsync_ShouldReturnLogsOrderedByTimestampDescending()
    {
        // Arrange
        _context.SystemLogs.AddRange(
            new SystemLog
            {
                Level = "Info",
                Message = "Old Log",
                Timestamp = DateTime.UtcNow.AddHours(-1),
            },
            new SystemLog
            {
                Level = "Error",
                Message = "New Log",
                Timestamp = DateTime.UtcNow,
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var logs = await _service.GetLogsAsync();

        // Assert
        Assert.Equal(2, logs.Count);
        Assert.Equal("New Log", logs[0].Message);
        Assert.Equal("Old Log", logs[1].Message);
    }

    [Fact]
    public async Task GetLogsAsync_WithLevelFilter_ShouldReturnFilteredLogs()
    {
        // Arrange
        _context.SystemLogs.AddRange(
            new SystemLog { Level = "Info", Message = "Info Log" },
            new SystemLog { Level = "Error", Message = "Error Log" }
        );
        await _context.SaveChangesAsync();

        // Act
        var logs = await _service.GetLogsAsync(level: "Error");

        // Assert
        Assert.Single(logs);
        Assert.Equal("Error Log", logs[0].Message);
    }

    [Fact]
    public async Task ClearLogsAsync_ShouldRemoveOldLogs()
    {
        // Arrange
        _context.SystemLogs.AddRange(
            new SystemLog
            {
                Level = "Info",
                Message = "Old Log",
                Timestamp = DateTime.UtcNow.AddDays(-31),
            },
            new SystemLog
            {
                Level = "Info",
                Message = "New Log",
                Timestamp = DateTime.UtcNow.AddDays(-1),
            }
        );
        await _context.SaveChangesAsync();

        // Act
        await _service.ClearLogsAsync(daysToKeep: 30);

        // Assert
        var logs = await _context.SystemLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("New Log", logs[0].Message);
    }
}
