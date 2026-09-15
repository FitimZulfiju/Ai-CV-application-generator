namespace AiCV.Domain;

public class AutomationQuery
{
    public int Id { get; set; }
    public int AutomationSettingsId { get; set; }
    public AutomationSettings? AutomationSettings { get; set; }
    public string Query { get; set; } = string.Empty;
    public string Provider { get; set; } = "Jobindex";
    public string? Location { get; set; }
    public string? Region { get; set; }
    public int JobAgeDays { get; set; } = 7;
    public int MaxResults { get; set; } = 20;
    public bool IsActive { get; set; } = true;
}
