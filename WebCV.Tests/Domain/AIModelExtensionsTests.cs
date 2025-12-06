using WebCV.Domain;
using Xunit;

namespace WebCV.Tests.Domain;

public class AIModelExtensionsTests
{
    [Theory]
    [InlineData(AIModel.Mistral7B, AIProvider.Local)]
    [InlineData(AIModel.Llama31_8B, AIProvider.Local)]
    [InlineData(AIModel.Phi3Mini, AIProvider.Local)]
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
    [InlineData(AIModel.Mistral7B, "Mistral 7B")]
    [InlineData(AIModel.Llama31_8B, "LLaMA 3.1 8B")]
    [InlineData(AIModel.Phi3Mini, "Phi-3 Mini 3.8B")]

    public void GetDisplayName_ReturnsCorrectDisplayName(AIModel model, string expectedName)
    {
        // Act
        var result = model.GetDisplayName();

        // Assert
        Assert.Equal(expectedName, result);
    }
}
