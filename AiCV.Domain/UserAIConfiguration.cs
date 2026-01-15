namespace AiCV.Domain;

public class UserAIConfiguration
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AIProvider Provider { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? ModelId { get; set; }
    public string? CostType { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
