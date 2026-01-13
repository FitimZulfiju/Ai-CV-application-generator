namespace AiCV.Infrastructure.Services
{
    public class JobApplicationOrchestrator(
        IJobPostScraper jobScraper,
        IAIServiceFactory aiServiceFactory,
        ICVService cvService,
        ILogger<JobApplicationOrchestrator> logger
    ) : IJobApplicationOrchestrator
    {
        private readonly IJobPostScraper _jobScraper = jobScraper;
        private readonly IAIServiceFactory _aiServiceFactory = aiServiceFactory;
        private readonly ICVService _cvService = cvService;
        private readonly ILogger<JobApplicationOrchestrator> _logger = logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public async Task<JobPosting> FetchJobDetailsAsync(string url)
        {
            return await _jobScraper.ScrapeJobPostingAsync(url);
        }

        public async Task<(
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
        )
        {
            // Get the AI service for the selected model
            var aiService = await _aiServiceFactory.GetServiceAsync(provider, userId, modelId);
            _logger.LogInformation(
                "Starting application generation for Job {JobTitle} using {ModelId} from provider {Provider}",
                job.Title,
                modelId ?? "default",
                provider
            );

            // Run cover letter and resume in parallel
            var coverLetterTask = aiService.GenerateCoverLetterAsync(profile, job, customPrompt);
            var resumeTask = aiService.GenerateTailoredResumeAsync(profile, job, customPrompt);

            await Task.WhenAll(coverLetterTask, resumeTask);

            var coverLetter = await coverLetterTask;
            var resumeResult = await resumeTask;

            // Generate email after cover letter is ready (needs cover letter content)
            var email = await aiService.GenerateApplicationEmailAsync(
                profile,
                job,
                coverLetter,
                customPrompt
            );

            return (coverLetter, resumeResult, email);
        }

        public async Task SaveApplicationAsync(
            string userId,
            JobPosting job,
            CandidateProfile profile,
            string coverLetter,
            CandidateProfile tailoredResume,
            string applicationEmail
        )
        {
            // Create a fresh JobPosting entity to avoid EF Core tracking issues
            // when saving multiple applications with the same job details
            var freshJobPosting = new JobPosting
            {
                Id = 0, // Ensure it's treated as a new entity
                Title = job.Title,
                CompanyName = job.CompanyName,
                Description = job.Description,
                Url = job.Url,
                DatePosted = DateTime.UtcNow,
            };

            var app = new GeneratedApplication
            {
                UserId = userId,
                JobPosting = freshJobPosting,
                CandidateProfileId = profile.Id,
                CoverLetterContent = coverLetter,
                TailoredResumeJson = JsonSerializer.Serialize(tailoredResume, JsonOptions),
                ApplicationEmailContent = applicationEmail,
                CreatedDate = DateTime.UtcNow,
            };

            await _cvService.SaveApplicationAsync(app);
        }
    }
}
