namespace AiCV.Tests.Services;

public class JobSearchProviderFactoryTests
{
    private static IJobSearchProvider CreateMockProvider(string name)
    {
        var mock = new Mock<IJobSearchProvider>();
        mock.SetupGet(p => p.ProviderName).Returns(name);
        return mock.Object;
    }

    [Fact]
    public void GetProvider_WithExactMatch_ReturnsProvider()
    {
        var jobindex = CreateMockProvider("Jobindex");
        var linkedIn = CreateMockProvider("LinkedIn");
        var factory = new JobSearchProviderFactory([jobindex, linkedIn]);

        var result = factory.GetProvider("Jobindex");

        Assert.NotNull(result);
        Assert.Equal("Jobindex", result.ProviderName);
    }

    [Fact]
    public void GetProvider_IsCaseInsensitive()
    {
        var linkedIn = CreateMockProvider("LinkedIn");
        var factory = new JobSearchProviderFactory([linkedIn]);

        var result = factory.GetProvider("linkedin");

        Assert.NotNull(result);
        Assert.Equal("LinkedIn", result.ProviderName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("UnknownProvider")]
    public void GetProvider_WithInvalidOrMissing_ReturnsNull(string? providerName)
    {
        var jobindex = CreateMockProvider("Jobindex");
        var factory = new JobSearchProviderFactory([jobindex]);

        var result = factory.GetProvider(providerName);

        Assert.Null(result);
    }

    [Fact]
    public void GetProviders_WithCommaSeparatedNames_ReturnsMatchingProviders()
    {
        var jobindex = CreateMockProvider("Jobindex");
        var linkedIn = CreateMockProvider("LinkedIn");
        var jSearch = CreateMockProvider("JSearch");
        var factory = new JobSearchProviderFactory([jobindex, linkedIn, jSearch]);

        var results = factory.GetProviders("Jobindex, LinkedIn").ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal("Jobindex", results[0].ProviderName);
        Assert.Equal("LinkedIn", results[1].ProviderName);
    }

    [Fact]
    public void GetProviders_WithWhitespaceAndMixedCase_ReturnsMatchingProviders()
    {
        var jobindex = CreateMockProvider("Jobindex");
        var jSearch = CreateMockProvider("JSearch");
        var factory = new JobSearchProviderFactory([jobindex, jSearch]);

        var results = factory.GetProviders("  jsearch ,  JOBINDEX  ").ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal("JSearch", results[0].ProviderName);
        Assert.Equal("Jobindex", results[1].ProviderName);
    }

    [Fact]
    public void GetProviders_IgnoresUnknownProvidersInList()
    {
        var jobindex = CreateMockProvider("Jobindex");
        var factory = new JobSearchProviderFactory([jobindex]);

        var results = factory.GetProviders("Jobindex, NonExistent, AnotherUnknown").ToList();

        var item = Assert.Single(results);
        Assert.Equal("Jobindex", item.ProviderName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetProviders_WithNullOrEmpty_ReturnsEmpty(string? providerNames)
    {
        var jobindex = CreateMockProvider("Jobindex");
        var factory = new JobSearchProviderFactory([jobindex]);

        var results = factory.GetProviders(providerNames).ToList();

        Assert.Empty(results);
    }
}

