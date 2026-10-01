namespace AiCV.Domain;

public class AutomationSettings
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }
    public bool IsEnabled { get; set; } = false;
    public string CronExpression { get; set; } = "0 6 * * *";
    public int MaxApplicationsPerRun { get; set; } = 10;
    public string Providers { get; set; } = "Jobindex";
    public DateTime? LastRunUtc { get; set; }
    public DateTime? NextRunUtc { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<AutomationQuery> Queries { get; set; } = [];
}
