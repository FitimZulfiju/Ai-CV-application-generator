namespace WebCV.Domain.Entities;

public class SystemLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Level { get; set; } = "Error"; // "Info", "Warning", "Error"

    [Required]
    public string Message { get; set; } = string.Empty;

    public string? StackTrace { get; set; }

    public string? Source { get; set; } // Class or Component name

    public string? RequestPath { get; set; } // URL path

    public string? UserId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
