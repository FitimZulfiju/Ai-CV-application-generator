namespace AiCV.Infrastructure.Services;

public class UserSmtpSettingsService(
    IDbContextFactory<ApplicationDbContext> contextFactory) : IUserSmtpSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;

    public async Task<UserSmtpSettings?> GetForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.UserSmtpSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    public async Task<UserSmtpSettings> UpdateAsync(string userId, UserSmtpSettings settings, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await context.UserSmtpSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (existing == null)
        {
            settings.UserId = userId;
            settings.CreatedAt = DateTime.UtcNow;
            context.UserSmtpSettings.Add(settings);
            existing = settings;
        }
        else
        {
            existing.SmtpHost = settings.SmtpHost;
            existing.SmtpPort = settings.SmtpPort;
            existing.SmtpUser = settings.SmtpUser;
            existing.SmtpPassword = settings.SmtpPassword;
            existing.EnableSsl = settings.EnableSsl;
            existing.FromEmail = settings.FromEmail;
            existing.FromName = settings.FromName;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        return existing;
    }
}

