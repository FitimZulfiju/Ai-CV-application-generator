namespace AiCV.Domain;

public class UserSmtpSettings
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public bool EnableSsl { get; set; } = true;
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

