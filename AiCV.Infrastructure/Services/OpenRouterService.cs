namespace AiCV.Infrastructure.Services
{
    public class OpenRouterService(HttpClient httpClient, string apiKey, string modelId)
        : IAIService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly string _apiKey = apiKey;
        private readonly string _modelId = modelId;
        private const string ApiUrl = "https://openrouter.ai/api/v1/chat/completions";

        public async Task<string> GenerateCoverLetterAsync(
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

            return await CallOpenRouterApiAsync(requestBody);
        }

        public async Task<TailoredResumeResult> GenerateTailoredResumeAsync(
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

            var jsonResponse = await CallOpenRouterApiAsync(requestBody);

            // Clean up JSON markdown code blocks if present
            var textResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();

            return AIResponseParser.ParseTailoredResume(textResponse, profile);
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
            {
                systemPrompt += $"\n\nAdditional Instructions: {customPrompt}";
            }

            var userPrompt = $"""
                Candidate Name: {profile.FullName}
                Position: {job.Title}
                Company: {job.CompanyName}

                Cover Letter Summary:
                {coverLetter[..Math.Min(500, coverLetter.Length)]}...

                Write a brief professional email to accompany this application.
                """;

            var requestBody = new
            {
                model = _modelId,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt },
                },
            };

            return await CallOpenRouterApiAsync(requestBody);
        }

        private async Task<string> CallOpenRouterApiAsync(object requestBody)
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
                throw new Exception($"OpenRouter API Error: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<OpenRouterResponse>();
            return result?.Choices?.FirstOrDefault()?.Message?.Content
                ?? "Error: No content generated.";
        }

        private static string BuildPrompt(
            CandidateProfile profile,
            JobPosting job,
            bool isResume = false
        )
        {
            return AIPromptBuilder.Build(profile, job, isResume);
        }

        private class OpenRouterResponse
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
}
