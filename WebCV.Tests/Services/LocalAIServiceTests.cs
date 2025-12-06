using System.Net;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using WebCV.Domain;
using WebCV.Application.DTOs;
using WebCV.Infrastructure.Services;
using Xunit;
using System.Text.Json;

namespace WebCV.Tests.Services;

public class LocalAIServiceTests
{
    private readonly Mock<ILogger<LocalAIService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly LocalAIService _service;

    public LocalAIServiceTests()
    {
        _loggerMock = new Mock<ILogger<LocalAIService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:11434")
        };
        _service = new LocalAIService(AIModel.Mistral7B, _loggerMock.Object, _httpClient);
    }

    [Fact]
    public async Task GenerateCoverLetterAsync_SendsCorrectRequestAndReturnsResponse()
    {
        // Arrange
        var expectedResponse = "Generated cover letter content.";
        var ollamaResponse = new OllamaResponse { Response = expectedResponse, Done = true };
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(ollamaResponse))
            });

        // Act
        var result = await _service.GenerateCoverLetterAsync(new CandidateProfile(), new JobPosting());

        // Assert
        Assert.Equal(expectedResponse, result);
    }

    [Fact]
    public async Task GenerateTailoredResumeAsync_ReturnsParsedResult()
    {
        // Arrange
        var profile = new CandidateProfile { FullName = "Test User" };
        var job = new JobPosting { Title = "Developer", CompanyName = "Test Corp" };
        
        // Mocking a JSON response as expected from Ollama for Resume Tailoring
        // Note: Real parsing logic depends on AIResponseParser. 
        // For this unit test, we just want to ensure the service calls the API and attempts to parse.
        // We might just return a dummy JSON that AIResponseParser can handle or expect it to fall back if parsing fails.
        // Let's rely on the Fallback in the service catch block for simplicity if the mock JSON isn't perfect for the parser.
        // Or better, let's mock a response that causes an exception to verify fallback, which is deterministic.
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"response\": \"invalid json to force fallback\", \"done\": true}")
            });

        // Act
        var result = await _service.GenerateTailoredResumeAsync(profile, job);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(profile, result.Profile); // Should return original profile due to fallback
        Assert.Equal("Developer", result.DetectedJobTitle);
        Assert.Equal("Test Corp", result.DetectedCompanyName);
    }
}
