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
                AIModel.Mistral7B => "mistral-7b",
                AIModel.Llama31_8B => "llama-3.1-8b",
                AIModel.Phi3Mini => "phi-3-mini",
                AIModel.Gpt4All => "gpt4all",
                _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
            };
        }

        public static AIModel FromModelString(string modelString)
        {
            return modelString?.ToLowerInvariant() switch
            {
                "gpt-4o" => AIModel.Gpt4o,
                "gemini-2.0-flash" => AIModel.Gemini20Flash,
                "mistral-7b" => AIModel.Mistral7B,
                "llama-3.1-8b" => AIModel.Llama31_8B,
                "phi-3-mini" => AIModel.Phi3Mini,
                "gpt4all" => AIModel.Gpt4All,
                _ => AIModel.Gpt4o // Default to GPT-4o
            };
        }

        public static AIProvider GetProvider(this AIModel model)
        {
            return model switch
            {
                AIModel.Gpt4o => AIProvider.OpenAI,
                AIModel.Gemini20Flash => AIProvider.GoogleGemini,
                AIModel.Mistral7B => AIProvider.Local,
                AIModel.Llama31_8B => AIProvider.Local,
                AIModel.Phi3Mini => AIProvider.Local,
                AIModel.Gpt4All => AIProvider.Local,
                _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
            };
        }

        public static string GetDisplayName(this AIModel model)
        {
            return model switch
            {
                AIModel.Gpt4o => "GPT-4o",
                AIModel.Gemini20Flash => "Gemini 2.0 Flash",
                AIModel.Mistral7B => "Mistral 7B",
                AIModel.Llama31_8B => "LLaMA 3.1 8B",
                AIModel.Phi3Mini => "Phi-3 Mini 3.8B",
                AIModel.Gpt4All => "GPT4All",
                _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
            };
        }
    }
}
