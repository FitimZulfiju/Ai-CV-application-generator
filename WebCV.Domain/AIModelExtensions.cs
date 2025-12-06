namespace WebCV.Domain
{
    public static class AIModelExtensions
    {
        public static string ToModelString(this AIModel model)
        {
            return model switch
            {
                AIModel.Gpt4o => "gpt-4o",
                AIModel.Gemini20Flash => "gemini-2.0-flash",
                _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
            };
        }

        public static AIModel FromModelString(string modelString)
        {
            return modelString?.ToLowerInvariant() switch
            {
                "gpt-4o" => AIModel.Gpt4o,
                "gemini-2.0-flash" => AIModel.Gemini20Flash,
                _ => AIModel.Gpt4o // Default to GPT-4o
            };
        }

        public static AIProvider GetProvider(this AIModel model)
        {
            return model switch
            {
                AIModel.Gpt4o => AIProvider.OpenAI,
                AIModel.Gemini20Flash => AIProvider.GoogleGemini,
                _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
            };
        }

        public static string GetDisplayName(this AIModel model)
        {
            return model switch
            {
                AIModel.Gpt4o => "GPT-4o",
                AIModel.Gemini20Flash => "Gemini 2.0 Flash",
                _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
            };
        }
    }
}
