namespace AiCV.Infrastructure.Services;

public class JobApplicationOrchestrator(
    IJobPostScraper jobScraper,
    IAIServiceFactory aiServiceFactory,
    ICVService cvService,
    IUserAIConfigurationService configService,
    IModelDiscoveryService discoveryService,
    ILogger<JobApplicationOrchestrator> logger
) : IJobApplicationOrchestrator
{
    private readonly IJobPostScraper _jobScraper = jobScraper;
    private readonly IAIServiceFactory _aiServiceFactory = aiServiceFactory;
    private readonly ICVService _cvService = cvService;
    private readonly IUserAIConfigurationService _configService = configService;
    private readonly IModelDiscoveryService _discoveryService = discoveryService;
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
        var modelsToTry = new List<string?>
        {
            modelId
        };

        Exception? lastException = null;

        for (int i = 0; i < modelsToTry.Count; i++)
        {
            var currentModelId = modelsToTry[i];
            try
            {
                var aiService = await _aiServiceFactory.GetServiceAsync(provider, userId, currentModelId);

                var coverLetterTask = aiService.GenerateCoverLetterAsync(profile, job, customPrompt);
                var resumeTask = aiService.GenerateTailoredResumeAsync(profile, job, customPrompt);

                await Task.WhenAll(coverLetterTask, resumeTask);

                var coverLetter = await coverLetterTask;
                var resumeResult = await resumeTask;

                var email = await aiService.GenerateApplicationEmailAsync(
                    profile,
                    job,
                    coverLetter,
                    customPrompt
                );

                return (coverLetter, resumeResult, email);
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (currentModelId == modelId && modelsToTry.Count == 1)
                {
                    try
                    {
                        var aiConfig = await _configService.GetActiveConfigurationAsync(userId);
                        if (aiConfig != null && !string.IsNullOrWhiteSpace(aiConfig.ApiKey))
                        {
                            var discoveryResult = await _discoveryService.DiscoverModelsAsync(provider, aiConfig.ApiKey);
                            if (discoveryResult.Success && discoveryResult.Models != null)
                            {
                                var dynamicModels = discoveryResult.Models
                                    .Select(m => m.ModelId)
                                    .Where(m => m != modelId)
                                    .ToList();

                                if (dynamicModels.Count > 0)
                                {
                                    modelsToTry.AddRange(dynamicModels);
                                    continue;
                                }
                            }
                        }
                    }
                    catch (Exception discoveryEx)
                    {
                        _logger.LogWarning(discoveryEx, "Failed to discover fallback models.");
                    }
                }

                if (currentModelId == modelsToTry.LastOrDefault())
                {
                    _logger.LogError("All available models failed to generate the application.");
                    throw;
                }
            }
        }

        throw lastException ?? new InvalidOperationException("Failed to generate application using any available model.");
    }

    public async Task SaveApplicationAsync(
        string userId,
        JobPosting job,
        CandidateProfile profile,
        string coverLetter,
        CandidateProfile tailoredResume,
        string applicationEmail,
        string template,
        string status = ApplicationStatus.PendingReview
    )
    {
        var freshJobPosting = new JobPosting
        {
            Id = 0,
            Title = job.Title,
            CompanyName = job.CompanyName,
            Description = job.Description,
            Url = job.Url,
            ApplyUrl = job.ApplyUrl,
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
            Template = template,
            Status = status,
            CreatedDate = DateTime.UtcNow,
        };

        await _cvService.SaveApplicationAsync(app);
    }
}
