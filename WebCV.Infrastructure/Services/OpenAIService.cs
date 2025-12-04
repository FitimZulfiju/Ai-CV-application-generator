namespace WebCV.Infrastructure.Services
{
    public class OpenAIService(string apiKey) : IAIService
    {
        private readonly ChatClient _chatClient = new("gpt-4o", new ApiKeyCredential(apiKey));

        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public async Task<string> GenerateCoverLetterAsync(CandidateProfile profile, JobPosting job)
        {
            var systemPrompt = AISystemPrompts.CoverLetterSystemPrompt;

            var userPrompt = BuildPrompt(profile, job);

            ChatCompletion completion = await _chatClient.CompleteChatAsync(
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            );

            return completion.Content[0].Text;
        }

        public async Task<Application.DTOs.TailoredResumeResult> GenerateTailoredResumeAsync(CandidateProfile profile, JobPosting job)
        {
            var systemPrompt = AISystemPrompts.ResumeTailoringSystemPrompt;

            var userPrompt = BuildPrompt(profile, job, isResume: true);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            ChatCompletion completion = await _chatClient.CompleteChatAsync(
                messages,
                new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() }
            );

            var textResponse = completion.Content[0].Text;

            return AIResponseParser.ParseTailoredResume(textResponse, profile);
        }

        private static string BuildPrompt(CandidateProfile profile, JobPosting job, bool isResume = false)
        {
            return AIPromptBuilder.Build(profile, job, isResume);
        }
    }
}
