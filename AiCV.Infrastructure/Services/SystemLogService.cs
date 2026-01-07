namespace AiCV.Infrastructure.Services;

public class SystemLogService(ApplicationDbContext context) : ISystemLogService
{
    private readonly ApplicationDbContext _context = context;

    public async Task LogErrorAsync(
        string message,
        string? stackTrace = null,
        string? source = null,
        string? requestPath = null,
        string? userId = null
    )
    {
        await LogAsync("Error", message, stackTrace, source, requestPath, userId);
    }

    public async Task LogWarningAsync(string message, string? source = null)
    {
        await LogAsync("Warning", message, null, source);
    }

    public async Task LogInfoAsync(string message, string? source = null)
    {
        await LogAsync("Info", message, null, source);
    }

    private async Task LogAsync(
        string level,
        string message,
        string? stackTrace = null,
        string? source = null,
        string? requestPath = null,
        string? userId = null
    )
    {
        try
        {
            var log = new SystemLog
            {
                Level = level,
                Message = message,
                StackTrace = stackTrace,
                Source = source,
                RequestPath = requestPath,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
            };

            _context.SystemLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Fail silently to avoid infinite error loops
        }
    }

    public async Task<List<SystemLog>> GetLogsAsync(
        int page = 1,
        int pageSize = 50,
        string? level = null
    )
    {
        var query = _context.SystemLogs.AsQueryable();

        if (!string.IsNullOrEmpty(level))
        {
            query = query.Where(l => l.Level == level);
        }

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalLogsCountAsync(string? level = null)
    {
        var query = _context.SystemLogs.AsQueryable();

        if (!string.IsNullOrEmpty(level))
        {
            query = query.Where(l => l.Level == level);
        }

        return await query.CountAsync();
    }

    public async Task ClearLogsAsync(int daysToKeep = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
        var oldLogs = _context.SystemLogs.Where(l => l.Timestamp < cutoff);
        _context.SystemLogs.RemoveRange(oldLogs);
        await _context.SaveChangesAsync();
    }
}
