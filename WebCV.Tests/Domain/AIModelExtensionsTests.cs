using WebCV.Domain;
using Xunit;

namespace WebCV.Tests.Domain;

public class AIModelExtensionsTests
{
    [Theory]
    [InlineData(AIModel.Claude35Haiku, AIProvider.Anthropic)]
    [InlineData(AIModel.Llama3370B, AIProvider.Groq)]
    [InlineData(AIModel.DeepSeekV3, AIProvider.DeepSeek)]
    [InlineData(AIModel.Gpt4o, AIProvider.OpenAI)]
    [InlineData(AIModel.Gemini20Flash, AIProvider.GoogleGemini)]
    public void GetProvider_ReturnsCorrectProvider(AIModel model, AIProvider expectedProvider)
    {
        // Act
        var result = model.GetProvider();

        // Assert
        Assert.Equal(expectedProvider, result);
    }

    [Theory]
    [InlineData(AIModel.Claude35Haiku, "Claude 3.5 Haiku")]
    [InlineData(AIModel.Llama3370B, "Llama 3.3 70B")]
    [InlineData(AIModel.DeepSeekV3, "DeepSeek V3")]
    [InlineData(AIModel.Gpt4o, "GPT-4o")]
    [InlineData(AIModel.Gemini20Flash, "Gemini 2.0 Flash")]
    public void GetDisplayName_ReturnsCorrectDisplayName(AIModel model, string expectedName)
    {
        // Act
        var result = model.GetDisplayName();

        // Assert
        Assert.Equal(expectedName, result);
    }
}
