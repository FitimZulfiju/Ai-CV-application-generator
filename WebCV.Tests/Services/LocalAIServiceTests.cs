using Microsoft.Extensions.Logging;
using Moq;
using WebCV.Application.DTOs;
using WebCV.Infrastructure.Services;
using Xunit;

namespace WebCV.Tests.Services;

public class LocalAIServiceTests
{
    private readonly Mock<ILogger<LocalAIService>> _loggerMock;
    private readonly LocalAIService _service;

    public LocalAIServiceTests()
    {
        _loggerMock = new Mock<ILogger<LocalAIService>>();
        _service = new LocalAIService(_loggerMock.Object);
    }

    [Fact]
    public async Task GenerateCoverLetterAsync_ReturnsPlaceholderText()
    {
        // Act
        var result = await _service.GenerateCoverLetterAsync(new CandidateProfile(), new JobPosting());

        // Assert
        Assert.NotNull(result);
        Assert.Contains("placeholder", result.ToLower());
    }

    [Fact]
    public async Task GenerateTailoredResumeAsync_ReturnsOriginalProfile()
    {
        // Arrange
        var profile = new CandidateProfile { Name = "Test User" };
        var job = new JobPosting { Title = "Developer", CompanyName = "Test Corp" };

        // Act
        var result = await _service.GenerateTailoredResumeAsync(profile, job);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(profile, result.Profile);
        Assert.Equal("Developer", result.DetectedJobTitle);
        Assert.Equal("Test Corp", result.DetectedCompanyName);
    }
}
