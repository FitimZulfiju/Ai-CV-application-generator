namespace WebCV.Domain
{
    public class UserSettings
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }
        
        // Encrypted API keys
        public string? OpenAIApiKey { get; set; }
        public string? GoogleGeminiApiKey { get; set; }
        public string? DefaultModel { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
