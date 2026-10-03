namespace AiCV.Infrastructure.Services;

public class OmniRouterService(
    HttpClient httpClient,
    string apiKey,
    string modelId,
    string? baseUrl,
    IStringLocalizer<AicvResources> localizer
) : AiServiceBase(localizer)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _apiKey = apiKey;
    private readonly string _modelId = modelId;
    private readonly string ApiUrl = string.IsNullOrWhiteSpace(baseUrl) ? "http://localhost:20128/v1/chat/completions" : baseUrl.TrimEnd('/') + "/chat/completions";

    protected override AIProvider Provider => AIProvider.OmniRouter;

    protected override async Task<HttpResponseMessage> SendProbeRequestAsync()
    {
        var requestBody = new
        {
            model = _modelId,
            messages = new[] { new { role = "user", content = "Test" } },
            max_tokens = 1,
        };

        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = JsonContent.Create(requestBody),
        };

        request.Headers.Add("Authorization", $"Bearer {_apiKey}");
        request.Headers.Add("HTTP-Referer", "https://github.com/FitimZulfiju/AiCV");
        request.Headers.Add("X-Title", "AiCV Application Generator");

        return await _httpClient.SendAsync(request);
    }

    public override async Task<string> GenerateCoverLetterAsync(
        CandidateProfile profile,
        JobPosting job,
        string? customPrompt = null
    )
    {
        var systemPrompt = AISystemPrompts.CoverLetterSystemPrompt;
        if (!string.IsNullOrWhiteSpace(customPrompt))
        {
            systemPrompt += $"\n\nAdditional Instructions: {customPrompt}";
        }

        var userPrompt = BuildPrompt(profile, job);

        var requestBody = new
        {
            model = _modelId,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
        };

        return AIResponseParser.ParseCoverLetter(await CallOmniRouterApiAsync(requestBody));
    }

    public override async Task<TailoredResumeResult> GenerateTailoredResumeAsync(
        CandidateProfile profile,
        JobPosting job,
        string? customPrompt = null
    )
    {
        var systemPrompt = AISystemPrompts.ResumeTailoringSystemPrompt;
        if (!string.IsNullOrWhiteSpace(customPrompt))
        {
            systemPrompt += $"\n\nAdditional Instructions: {customPrompt}";
        }

        var userPrompt = BuildPrompt(profile, job, isResume: true);

        var requestBody = new
        {
            model = _modelId,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
        };

        var jsonResponse = await CallOmniRouterApiAsync(requestBody);

        // Clean up JSON markdown code blocks if present
        var textResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();

        return AIResponseParser.ParseTailoredResume(textResponse, profile);
    }

    public override async Task<string> GenerateApplicationEmailAsync(
        CandidateProfile profile,
        JobPosting job,
        string coverLetter,
        string? customPrompt = null
    )
    {
        var systemPrompt = AISystemPrompts.ApplicationEmailSystemPrompt;
        if (!string.IsNullOrWhiteSpace(customPrompt))
        {
            systemPrompt += $"\n\nAdditional Instructions: {customPrompt}";
        }

        var userPrompt = BuildEmailPrompt(profile, job, coverLetter);

        var requestBody = new
        {
            model = _modelId,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
        };

        return await CallOmniRouterApiAsync(requestBody);
    }

    private async Task<string> CallOmniRouterApiAsync(object requestBody)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = JsonContent.Create(requestBody),
        };

        request.Headers.Add("Authorization", $"Bearer {_apiKey}");
        request.Headers.Add("HTTP-Referer", "https://github.com/FitimZulfiju/AiCV");
        request.Headers.Add("X-Title", "AiCV Application Generator");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception(
                AIErrorMapper.MapError(
                    AIProvider.OmniRouter,
                    error,
                    response.StatusCode,
                    _localizer
                )
            );
        }

        var result = await response.Content.ReadFromJsonAsync<OmniRouterResponse>();
        return result?.Choices?.FirstOrDefault()?.Message?.Content
            ?? "Error: No content generated.";
    }

    private class OmniRouterResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    private class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
