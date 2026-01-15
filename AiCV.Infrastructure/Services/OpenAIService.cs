namespace AiCV.Infrastructure.Services;

public class OpenAIService(
    string apiKey,
    string modelId,
    IStringLocalizer<AicvResources> localizer
) : AiServiceBase(localizer)
{
    private readonly ChatClient _chatClient = new(modelId, new ApiKeyCredential(apiKey));

    protected override AIProvider Provider => AIProvider.OpenAI;
    protected override bool UseHttpProbing => false;

    protected override async Task SendProbeActionAsync()
    {
        await _chatClient.CompleteChatAsync(
            [new UserChatMessage("Test")],
            new ChatCompletionOptions { MaxOutputTokenCount = 1 }
        );
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

        ChatCompletion completion = await _chatClient.CompleteChatAsync(
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        );

        return completion.Content[0].Text;
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

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt),
        };

        ChatCompletion completion = await _chatClient.CompleteChatAsync(
            messages,
            new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
            }
        );

        var textResponse = completion.Content[0].Text;

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

        var userPrompt = $"""
            Candidate Name: {profile.FullName}
            Position: {job.Title}
            Company: {job.CompanyName}

            Cover Letter Summary:
            {coverLetter[..Math.Min(500, coverLetter.Length)]}...

            Write a brief professional email to accompany this application.
            """;

        ChatCompletion completion = await _chatClient.CompleteChatAsync(
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        );

        return completion.Content[0].Text;
    }
}
