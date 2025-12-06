namespace WebCV.Infrastructure.Services;

public class DeepSeekService(HttpClient httpClient, string apiKey) : IAIService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _apiKey = apiKey;
    private readonly string ApiUrl = "https://api.deepseek.com/v1/chat/completions";
    private const string Model = "deepseek-chat";

    public async Task<string> GenerateCoverLetterAsync(CandidateProfile profile, JobPosting job)
    {
        var prompt = $"{AISystemPrompts.CoverLetterSystemPrompt}\n\n{BuildPrompt(profile, job)}";
        return await CallDeepSeekApiAsync(prompt);
    }

    public async Task<TailoredResumeResult> GenerateTailoredResumeAsync(CandidateProfile profile, JobPosting job)
    {
        var prompt = $"{AISystemPrompts.ResumeTailoringSystemPrompt}\n\n{BuildPrompt(profile, job)}";
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
                DetectedCompanyName = job.CompanyName ?? "Not specified"
            };
        }
    }

    private async Task<string> CallDeepSeekApiAsync(string prompt)
    {
        var requestBody = new
        {
            model = Model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.7
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"DeepSeek API Error: {response.StatusCode} - {responseContent}");
        }

        var result = JsonSerializer.Deserialize<DeepSeekResponse>(responseContent);
        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    private static string BuildPrompt(CandidateProfile profile, JobPosting job)
    {
        return $@"
            Candidate Profile:
            Name: {profile.FullName}
            Email: {profile.Email}
            Phone: {profile.PhoneNumber}
            Professional Summary: {profile.ProfessionalSummary}
            Skills: {string.Join(", ", profile.Skills.Select(s => s.Name))}

            Job Posting:
            Company: {job.CompanyName}
            Position: {job.Title}
            Description: {job.Description}
";
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
