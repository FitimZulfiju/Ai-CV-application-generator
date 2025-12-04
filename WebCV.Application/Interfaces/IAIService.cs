namespace WebCV.Application.Interfaces
{
    public interface IAIService
    {
        Task<string> GenerateCoverLetterAsync(CandidateProfile profile, JobPosting job);
        Task<TailoredResumeResult> GenerateTailoredResumeAsync(CandidateProfile profile, JobPosting job);
    }
}
