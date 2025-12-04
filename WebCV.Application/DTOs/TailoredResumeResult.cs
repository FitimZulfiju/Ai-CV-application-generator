namespace WebCV.Application.DTOs
{
    public class TailoredResumeResult
    {
        public CandidateProfile Profile { get; set; } = new();
        public string? DetectedCompanyName { get; set; }
        public string? DetectedJobTitle { get; set; }
    }
}
