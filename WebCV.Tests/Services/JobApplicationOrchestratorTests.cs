using Moq;
using System.Text.Json;
using WebCV.Domain;
using WebCV.Application.Interfaces;
using WebCV.Application.DTOs;
using WebCV.Infrastructure.Services;
using Xunit;
using Microsoft.Extensions.Logging;

namespace WebCV.Tests.Services;

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
        var url = "https://example.com/job";
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
        var userId = "user1";
        var provider = AIProvider.OpenAI;
        var profile = new CandidateProfile();
        var job = new JobPosting();
        
        var expectedCoverLetter = "Dear Hiring Manager...";
        var expectedResume = new TailoredResumeResult { Profile = new CandidateProfile { FullName = "Tailored" } };

        _mockAiFactory.Setup(f => f.GetServiceAsync(provider, userId))
            .ReturnsAsync(_mockAiService.Object);

        _mockAiService.Setup(a => a.GenerateCoverLetterAsync(profile, job))
            .ReturnsAsync(expectedCoverLetter);
        
        _mockAiService.Setup(a => a.GenerateTailoredResumeAsync(profile, job))
            .ReturnsAsync(expectedResume);

        // Act
        var (coverLetter, resumeResult) = await _orchestrator.GenerateApplicationAsync(userId, provider, profile, job);

        // Assert
        Assert.Equal(expectedCoverLetter, coverLetter);
        Assert.Equal(expectedResume, resumeResult);
        
        _mockAiFactory.Verify(f => f.GetServiceAsync(provider, userId), Times.Once);
        _mockAiService.Verify(a => a.GenerateCoverLetterAsync(profile, job), Times.Once);
        _mockAiService.Verify(a => a.GenerateTailoredResumeAsync(profile, job), Times.Once);
    }

    [Fact]
    public async Task SaveApplicationAsync_ShouldSaveToCvService()
    {
        // Arrange
        var userId = "user1";
        var job = new JobPosting { Title = "Job" };
        var profile = new CandidateProfile { Id = 1 };
        var coverLetter = "Cover Letter";
        var tailoredResume = new CandidateProfile { FullName = "Tailored" };

        // Act
        await _orchestrator.SaveApplicationAsync(userId, job, profile, coverLetter, tailoredResume);

        // Assert
        _mockCvService.Verify(s => s.SaveApplicationAsync(It.Is<GeneratedApplication>(app => 
            app.UserId == userId &&
            app.JobPosting == job &&
            app.CandidateProfileId == profile.Id &&
            app.CoverLetterContent == coverLetter &&
            !string.IsNullOrEmpty(app.TailoredResumeJson)
        )), Times.Once);
    }
}
