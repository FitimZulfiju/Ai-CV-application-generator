namespace WebCV.Domain
{
    public static class AIModelExtensions
    {
        public static string ToModelString(this AIModel model)
        {
            return model switch
            {
                AIModel.Gpt4o => "gpt-4o",
                AIModel.Gemini20Flash => "gemini-2.0-flash-exp",
                AIModel.Claude35Haiku => "claude-3-5-haiku-20241022",
                AIModel.Llama3370B => "llama-3.3-70b-versatile",
                AIModel.DeepSeekV3 => "deepseek-chat",
                _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
            };
        }

        public static AIModel FromModelString(string modelString)
        {
            return modelString?.ToLowerInvariant() switch
            {
                "gpt-4o" => AIModel.Gpt4o,
                "gemini-2.0-flash-exp" => AIModel.Gemini20Flash,
                "claude-3-5-haiku-20241022" => AIModel.Claude35Haiku,
                "llama-3.3-70b-versatile" => AIModel.Llama3370B,
                "deepseek-chat" => AIModel.DeepSeekV3,
                _ => AIModel.Gpt4o // Default to GPT-4o
            };
        }

        public static AIProvider GetProvider(this AIModel model)
        {
            return model switch
            {
                AIModel.Gpt4o => AIProvider.OpenAI,
                AIModel.Gemini20Flash => AIProvider.GoogleGemini,
                AIModel.Claude35Haiku => AIProvider.Anthropic,
                AIModel.Llama3370B => AIProvider.Groq,
                AIModel.DeepSeekV3 => AIProvider.DeepSeek,
                _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
            };
        }

        public static string GetDisplayName(this AIModel model)
        {
            return model switch
            {
                AIModel.Gpt4o => "GPT-4o",
                AIModel.Gemini20Flash => "Gemini 2.0 Flash",
                AIModel.Claude35Haiku => "Claude 3.5 Haiku",
                AIModel.Llama3370B => "Llama 3.3 70B",
                AIModel.DeepSeekV3 => "DeepSeek V3",
                _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
            };
        }
    }
}
