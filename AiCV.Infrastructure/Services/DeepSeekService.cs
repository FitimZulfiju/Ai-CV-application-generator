namespace AiCV.Infrastructure.Services;

public class DeepSeekService(HttpClient httpClient, string apiKey, string modelId) : IAIService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _apiKey = apiKey;
    private readonly string ApiUrl = "https://api.deepseek.com/v1/chat/completions";
    private readonly string _modelId = modelId;

    public async Task<string> GenerateCoverLetterAsync(
        CandidateProfile profile,
        JobPosting job,
        string? customPrompt = null
    )
    {
        var systemPrompt = AISystemPrompts.CoverLetterSystemPrompt;
        if (!string.IsNullOrWhiteSpace(customPrompt))
            systemPrompt += $"\n\nAdditional Instructions: {customPrompt}";
        var prompt = $"{systemPrompt}\n\n{AIPromptBuilder.Build(profile, job)}";
        return await CallDeepSeekApiAsync(prompt);
    }

    public async Task<TailoredResumeResult> GenerateTailoredResumeAsync(
        CandidateProfile profile,
        JobPosting job,
        string? customPrompt = null
    )
    {
        var systemPrompt = AISystemPrompts.ResumeTailoringSystemPrompt;
        if (!string.IsNullOrWhiteSpace(customPrompt))
            systemPrompt += $"\n\nAdditional Instructions: {customPrompt}";
        var prompt = $"{systemPrompt}\n\n{AIPromptBuilder.Build(profile, job, isResume: true)}";
        var jsonResponse = await CallDeepSeekApiAsync(prompt);

        try
        {
            return AIResponseParser.ParseTailoredResume(jsonResponse, profile);
        }
        catch
        {
            // Fallback: return original profile if parsing fails
            return new TailoredResumeResult
            {
                Profile = profile,
                DetectedJobTitle = job.Title ?? "Not specified",
                DetectedCompanyName = job.CompanyName ?? "Not specified",
            };
        }
    }

    public async Task<string> GenerateApplicationEmailAsync(
        CandidateProfile profile,
        JobPosting job,
        string coverLetter,
        string? customPrompt = null
    )
    {
        var systemPrompt = AISystemPrompts.ApplicationEmailSystemPrompt;
        if (!string.IsNullOrWhiteSpace(customPrompt))
            systemPrompt += $"\n\nAdditional Instructions: {customPrompt}";

        var userPrompt = $"""
            Candidate Name: {profile.FullName}
            Position: {job.Title}
            Company: {job.CompanyName}

            Cover Letter Summary:
            {coverLetter[..Math.Min(500, coverLetter.Length)]}...

            Write a brief professional email to accompany this application.
            """;

        var prompt = $"{systemPrompt}\n\n{userPrompt}";
        return await CallDeepSeekApiAsync(prompt);
    }

    private async Task<string> CallDeepSeekApiAsync(string prompt)
    {
        var requestBody = new
        {
            model = _modelId,
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.7,
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"DeepSeek API Error: {response.StatusCode} - {responseContent}"
            );
        }

        var result = JsonSerializer.Deserialize<DeepSeekResponse>(responseContent);
        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    private class DeepSeekResponse
    {
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        public Message? Message { get; set; }
    }

    private class Message
    {
        public string? Content { get; set; }
    }
}
