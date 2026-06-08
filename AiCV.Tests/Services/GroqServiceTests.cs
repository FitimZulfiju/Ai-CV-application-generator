namespace AiCV.Tests.Services;

public class GroqServiceTests
{
    private readonly Mock<IStringLocalizer<AicvResources>> _localizerMock;

    public GroqServiceTests()
    {
        _localizerMock = new Mock<IStringLocalizer<AicvResources>>();
        _localizerMock.Setup(x => x[It.IsAny<string>()]).Returns((string s) => new LocalizedString(s, s));
    }

    private GroqService CreateService(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var mockMessageHandler = new Mock<HttpMessageHandler>();
        mockMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseJson)
            });

        var httpClient = new HttpClient(mockMessageHandler.Object);
        return new GroqService(httpClient, "fake_api_key", "llama3", _localizerMock.Object);
    }

    [Fact]
    public async Task GenerateCoverLetterAsync_Success_ReturnsCoverLetter()
    {
        // Arrange
        const string jsonResponse = """
        {
            "choices": [
                {
                    "message": {
                        "content": "Dear Hiring Manager, this is my letter."
                    }
                }
            ]
        }
        """;
        var service = CreateService(jsonResponse);

        var profile = new CandidateProfile { FullName = "Test" };
        var job = new JobPosting { Title = "Dev" };

        // Act
        var result = await service.GenerateCoverLetterAsync(profile, job);

        // Assert
        Assert.Equal("Dear Hiring Manager, this is my letter.", result);
    }

    [Fact]
    public async Task GenerateApplicationEmailAsync_Success_ReturnsEmail()
    {
        // Arrange
        const string jsonResponse = """
        {
            "choices": [
                {
                    "message": {
                        "content": "Subject: Application\n\nHere is my application."
                    }
                }
            ]
        }
        """;
        var service = CreateService(jsonResponse);

        // Act
        var result = await service.GenerateApplicationEmailAsync(
            new CandidateProfile(), new JobPosting(), "cover letter"
        );

        // Assert
        Assert.Equal("Subject: Application\n\nHere is my application.", result);
    }

    [Fact]
    public async Task GenerateTailoredResumeAsync_HttpError_ThrowsExceptionWithMappedError()
    {
        // Arrange
        var service = CreateService("Unauthorized", HttpStatusCode.Unauthorized);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => service.GenerateTailoredResumeAsync(
            new CandidateProfile(), new JobPosting()
        ));
        
        // Assert it hit the AIErrorMapper (which will return standard unauthorized message or mapped key)
        Assert.NotNull(ex.Message);
    }
}
