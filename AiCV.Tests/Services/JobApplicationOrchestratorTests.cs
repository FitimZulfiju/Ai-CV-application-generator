using AiCV.Application.DTOs;
using AiCV.Application.Interfaces;
using AiCV.Domain;
using AiCV.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace AiCV.Tests.Services;

public class JobApplicationOrchestratorTests
{
    private readonly Mock<IJobPostScraper> _mockScraper;
    private readonly Mock<IAIServiceFactory> _mockAiFactory;
    private readonly Mock<ICVService> _mockCvService;
    private readonly Mock<IAIService> _mockAiService;
    private readonly Mock<ILogger<JobApplicationOrchestrator>> _mockLogger;
    private readonly JobApplicationOrchestrator _orchestrator;

    public JobApplicationOrchestratorTests()
    {
        _mockScraper = new Mock<IJobPostScraper>();
        _mockAiFactory = new Mock<IAIServiceFactory>();
        _mockCvService = new Mock<ICVService>();
        _mockAiService = new Mock<IAIService>();
        _mockLogger = new Mock<ILogger<JobApplicationOrchestrator>>();

        _orchestrator = new JobApplicationOrchestrator(
            _mockScraper.Object,
            _mockAiFactory.Object,
            _mockCvService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task FetchJobDetailsAsync_ShouldCallScraper()
    {
        // Arrange
        const string url = "https://example.com/job";
        var expectedJob = new JobPosting { Title = "Test Job" };
        _mockScraper.Setup(s => s.ScrapeJobPostingAsync(url)).ReturnsAsync(expectedJob);

        // Act
        var result = await _orchestrator.FetchJobDetailsAsync(url);

        // Assert
        Assert.Equal(expectedJob, result);
        _mockScraper.Verify(s => s.ScrapeJobPostingAsync(url), Times.Once);
    }

    [Fact]
    public async Task GenerateApplicationAsync_ShouldOrchestrateAIAndReturnResults()
    {
        // Arrange
        const string userId = "user1";
        const AIProvider provider = AIProvider.OpenAI;
        var profile = new CandidateProfile();
        var job = new JobPosting();

        const string expectedCoverLetter = "Dear Hiring Manager...";
        var expectedResume = new TailoredResumeResult
        {
            Profile = new CandidateProfile { FullName = "Tailored" },
        };

        _mockAiFactory
            .Setup(f =>
                f.GetServiceAsync(It.IsAny<AIProvider>(), It.IsAny<string>(), It.IsAny<string?>())
            )
            .ReturnsAsync(_mockAiService.Object);

        _mockAiService
            .Setup(a =>
                a.GenerateCoverLetterAsync(
                    It.IsAny<CandidateProfile>(),
                    It.IsAny<JobPosting>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(expectedCoverLetter);

        _mockAiService
            .Setup(a =>
                a.GenerateTailoredResumeAsync(
                    It.IsAny<CandidateProfile>(),
                    It.IsAny<JobPosting>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(expectedResume);

        const string expectedEmail = "Dear Hiring Manager, I am excited to apply...";
        _mockAiService
            .Setup(a =>
                a.GenerateApplicationEmailAsync(
                    It.IsAny<CandidateProfile>(),
                    It.IsAny<JobPosting>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()
                )
            )
            .ReturnsAsync(expectedEmail);

        // Act
        var (coverLetter, resumeResult, applicationEmail) =
            await _orchestrator.GenerateApplicationAsync(userId, provider, profile, job);

        // Assert
        Assert.Equal(expectedCoverLetter, coverLetter);
        Assert.Equal(expectedResume, resumeResult);
        Assert.Equal(expectedEmail, applicationEmail);

        _mockAiFactory.Verify(
            f => f.GetServiceAsync(provider, userId, It.IsAny<string?>()),
            Times.Once
        );
        _mockAiService.Verify(
            a => a.GenerateCoverLetterAsync(profile, job, It.IsAny<string>()),
            Times.Once
        );
        _mockAiService.Verify(
            a => a.GenerateTailoredResumeAsync(profile, job, It.IsAny<string>()),
            Times.Once
        );
        _mockAiService.Verify(
            a =>
                a.GenerateApplicationEmailAsync(
                    profile,
                    job,
                    expectedCoverLetter,
                    It.IsAny<string>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task SaveApplicationAsync_ShouldSaveToCvService()
    {
        // Arrange
        var userId = "user1";
        var job = new JobPosting
        {
            Title = "Job Title",
            CompanyName = "Test Company",
            Description = "Job Description",
            Url = "https://example.com/job",
        };
        var profile = new CandidateProfile { Id = 1 };
        var coverLetter = "Cover Letter";
        var tailoredResume = new CandidateProfile { FullName = "Tailored" };
        var applicationEmail = "Dear Hiring Manager...";

        // Act
        await _orchestrator.SaveApplicationAsync(
            userId,
            job,
            profile,
            coverLetter,
            tailoredResume,
            applicationEmail
        );

        // Assert - Note: The orchestrator creates a fresh JobPosting entity to avoid EF Core tracking issues
        // so we verify properties match instead of reference equality
        _mockCvService.Verify(
            s =>
                s.SaveApplicationAsync(
                    It.Is<GeneratedApplication>(app =>
                        app.UserId == userId
                        && app.JobPosting != null
                        && app.JobPosting.Title == job.Title
                        && app.JobPosting.CompanyName == job.CompanyName
                        && app.JobPosting.Description == job.Description
                        && app.JobPosting.Url == job.Url
                        && app.JobPosting.Id == 0 // Fresh entity should have Id = 0
                        && app.CandidateProfileId == profile.Id
                        && app.CoverLetterContent == coverLetter
                        && !string.IsNullOrEmpty(app.TailoredResumeJson)
                        && app.ApplicationEmailContent == applicationEmail
                    )
                ),
            Times.Once
        );
    }
}
