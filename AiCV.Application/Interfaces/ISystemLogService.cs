namespace AiCV.Application.Interfaces;

public interface ISystemLogService
{
    Task LogErrorAsync(
        string message,
        string? stackTrace = null,
        string? source = null,
        string? requestPath = null,
        string? userId = null
    );
    Task LogWarningAsync(string message, string? source = null);
    Task LogInfoAsync(string message, string? source = null);
    Task<List<SystemLog>> GetLogsAsync(int page = 1, int pageSize = 50, string? level = null);
    Task<int> GetTotalLogsCountAsync(string? level = null);
    Task ClearLogsAsync(int daysToKeep = 30);
}
