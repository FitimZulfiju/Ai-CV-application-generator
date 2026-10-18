namespace AiCV.Tests.Services;

public class ColorHelperTests
{
    [Theory]
    [InlineData("Jobindex", Color.Primary)]
    [InlineData("jobindex.dk", Color.Primary)]
    [InlineData("LinkedIn", Color.Info)]
    [InlineData("linkedin.com", Color.Info)]
    [InlineData("Indeed", Color.Tertiary)]
    [InlineData("indeed.com", Color.Tertiary)]
    [InlineData("Glassdoor", Color.Success)]
    [InlineData("glassdoor.com", Color.Success)]
    [InlineData("JSearch", Color.Warning)]
    [InlineData("Google", Color.Secondary)]
    public void GetProviderColor_KnownJobProviders_ReturnsExpectedColor(string provider, Color expectedColor)
    {
        var result = ColorHelper.GetProviderColor(provider);
        Assert.Equal(expectedColor, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetProviderColor_NullOrWhiteSpace_ReturnsDefaultColor(string? provider)
    {
        var result = ColorHelper.GetProviderColor(provider);
        Assert.Equal(Color.Default, result);
    }

    [Fact]
    public void GetProviderColor_UnknownProviders_AreDeterministicAndNonDefault()
    {
        var color1 = ColorHelper.GetProviderColor("TheHub");
        var color2 = ColorHelper.GetProviderColor("TheHub");
        var color3 = ColorHelper.GetProviderColor("Monster");

        Assert.Equal(color1, color2);
        Assert.NotEqual(Color.Default, color1);
        Assert.NotEqual(Color.Default, color3);
    }

    [Theory]
    [InlineData(AIProvider.GoogleGemini, Color.Primary)]
    [InlineData(AIProvider.OpenAI, Color.Success)]
    [InlineData(AIProvider.Claude, Color.Warning)]
    [InlineData(AIProvider.Groq, Color.Info)]
    [InlineData(AIProvider.DeepSeek, Color.Secondary)]
    [InlineData(AIProvider.OpenRouter, Color.Dark)]
    [InlineData(AIProvider.OmniRouter, Color.Dark)]
    [InlineData(AIProvider.Custom, Color.Surface)]
    public void GetProviderColor_AIProvider_ReturnsExpectedColor(AIProvider provider, Color expectedColor)
    {
        var result = ColorHelper.GetProviderColor(provider);
        Assert.Equal(expectedColor, result);
    }

    [Theory]
    [InlineData(ApplicationStatus.PendingReview, Color.Warning)]
    [InlineData(ApplicationStatus.Approved, Color.Info)]
    [InlineData(ApplicationStatus.AutoApplied, Color.Success)]
    [InlineData(ApplicationStatus.Applied, Color.Success)]
    [InlineData(ApplicationStatus.Rejected, Color.Error)]
    public void GetStatusColor_ApplicationStatus_ReturnsExpectedColor(string status, Color expectedColor)
    {
        var result = ColorHelper.GetStatusColor(status);
        Assert.Equal(expectedColor, result);
    }

    [Theory]
    [InlineData("Jobindex", Variant.Filled)]
    [InlineData("LinkedIn", Variant.Outlined)]
    [InlineData("Indeed", Variant.Outlined)]
    [InlineData("Glassdoor", Variant.Filled)]
    [InlineData("JSearch", Variant.Filled)]
    [InlineData("Google", Variant.Outlined)]
    public void GetProviderVariant_KnownJobProviders_ReturnsExpectedVariant(string provider, Variant expectedVariant)
    {
        var result = ColorHelper.GetProviderVariant(provider);
        Assert.Equal(expectedVariant, result);
    }

    [Theory]
    [InlineData(null, Variant.Outlined)]
    [InlineData("", Variant.Outlined)]
    public void GetProviderVariant_NullOrEmpty_ReturnsOutlined(string? provider, Variant expectedVariant)
    {
        var result = ColorHelper.GetProviderVariant(provider);
        Assert.Equal(expectedVariant, result);
    }

    [Theory]
    [InlineData("LinkedIn", Icons.Custom.Brands.LinkedIn)]
    [InlineData("Jobindex", Icons.Material.Filled.Work)]
    [InlineData("Indeed", Icons.Material.Filled.Search)]
    [InlineData("Glassdoor", Icons.Material.Filled.Star)]
    [InlineData("JSearch", Icons.Material.Filled.TravelExplore)]
    [InlineData("Google", Icons.Custom.Brands.Google)]
    public void GetProviderIcon_KnownJobProviders_ReturnsExpectedIcon(string provider, string expectedIcon)
    {
        var result = ColorHelper.GetProviderIcon(provider);
        Assert.Equal(expectedIcon, result);
    }
}

