namespace WebCV.Domain
{
    public class GeneratedApplication
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }
        
        public int JobPostingId { get; set; }
        public JobPosting? JobPosting { get; set; }
        
        public int CandidateProfileId { get; set; }
        public CandidateProfile? CandidateProfile { get; set; }
        
        public string CoverLetterContent { get; set; } = string.Empty;
        public string TailoredResumeJson { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
