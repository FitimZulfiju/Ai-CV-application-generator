namespace WebCV.Infrastructure.Services
{
    public class JobApplicationOrchestrator(
        IJobPostScraper jobScraper,
        IAIServiceFactory aiServiceFactory,
        ICVService cvService) : IJobApplicationOrchestrator
    {
        private readonly IJobPostScraper _jobScraper = jobScraper;
        private readonly IAIServiceFactory _aiServiceFactory = aiServiceFactory;
        private readonly ICVService _cvService = cvService;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public async Task<JobPosting> FetchJobDetailsAsync(string url)
        {
            return await _jobScraper.ScrapeJobPostingAsync(url);
        }

        public async Task<(string CoverLetter, TailoredResumeResult ResumeResult)> GenerateApplicationAsync(
            string userId,
            AIProvider provider,
            CandidateProfile profile,
            JobPosting job)
        {
            var aiService = await _aiServiceFactory.GetServiceAsync(provider, userId);

            var coverLetterTask = aiService.GenerateCoverLetterAsync(profile, job);
            var resumeTask = aiService.GenerateTailoredResumeAsync(profile, job);

            await Task.WhenAll(coverLetterTask, resumeTask);

            return (await coverLetterTask, await resumeTask);
        }

        public async Task SaveApplicationAsync(
            string userId,
            JobPosting job,
            CandidateProfile profile,
            string coverLetter,
            CandidateProfile tailoredResume)
        {
            var app = new GeneratedApplication
            {
                UserId = userId,
                JobPosting = job,
                CandidateProfileId = profile.Id,
                CoverLetterContent = coverLetter,
                TailoredResumeJson = JsonSerializer.Serialize(tailoredResume, JsonOptions),
                CreatedDate = DateTime.UtcNow
            };

            await _cvService.SaveApplicationAsync(app);
        }
    }
}
