namespace WebCV.Application.Interfaces
{
    public interface IJobApplicationOrchestrator
    {
        Task<JobPosting> FetchJobDetailsAsync(string url);
        Task<(string CoverLetter, TailoredResumeResult ResumeResult)> GenerateApplicationAsync(string userId, AIProvider provider, CandidateProfile profile, JobPosting job);
        Task SaveApplicationAsync(string userId, JobPosting job, CandidateProfile profile, string coverLetter, CandidateProfile tailoredResume);
    }
}
