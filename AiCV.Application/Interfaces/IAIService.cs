namespace AiCV.Application.Interfaces
{
    public interface IAIService
    {
        Task<string> GenerateCoverLetterAsync(
            CandidateProfile profile,
            JobPosting job,
            string? customPrompt = null
        );
        Task<TailoredResumeResult> GenerateTailoredResumeAsync(
            CandidateProfile profile,
            JobPosting job,
            string? customPrompt = null
        );
        Task<string> GenerateApplicationEmailAsync(
            CandidateProfile profile,
            JobPosting job,
            string coverLetter,
            string? customPrompt = null
        );
    }
}
