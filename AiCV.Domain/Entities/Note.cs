namespace AiCV.Domain.Entities;

public class Note
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Title { get; set; }

    public string? Content { get; set; }

    [MaxLength(20)]
    public string Color { get; set; } = "default";

    public bool IsPinned { get; set; }

    public bool IsArchived { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User User { get; set; } = null!;
}
