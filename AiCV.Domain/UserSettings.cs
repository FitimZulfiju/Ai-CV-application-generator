namespace AiCV.Domain;

public class UserSettings
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }

    // Encrypted API keys
    public string? OpenAIApiKey { get; set; }
    public string? GoogleGeminiApiKey { get; set; }
    public string? ClaudeApiKey { get; set; }
    public string? GroqApiKey { get; set; }
    public string? DeepSeekApiKey { get; set; }
    public string? OpenRouterApiKey { get; set; }

    public AIProvider DefaultProvider { get; set; } = AIProvider.OpenAI;
    public string? DefaultModelId { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
}
