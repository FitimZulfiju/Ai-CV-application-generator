namespace AiCV.Application.Interfaces
{
    public interface IJobApplicationOrchestrator
    {
        Task<JobPosting> FetchJobDetailsAsync(string url);
        Task<(
            string CoverLetter,
            TailoredResumeResult ResumeResult,
            string ApplicationEmail
        )> GenerateApplicationAsync(
            string userId,
            AIProvider provider,
            CandidateProfile profile,
            JobPosting job,
            string? modelId = null,
            string? customPrompt = null
        );
        Task SaveApplicationAsync(
            string userId,
            JobPosting job,
            CandidateProfile profile,
            string coverLetter,
            CandidateProfile tailoredResume,
            string applicationEmail
        );
    }
}
