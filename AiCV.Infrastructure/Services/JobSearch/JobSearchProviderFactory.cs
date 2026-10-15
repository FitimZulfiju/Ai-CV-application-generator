namespace AiCV.Infrastructure.Services.JobSearch;

public class JobSearchProviderFactory(IEnumerable<IJobSearchProvider> providers)
{
    private readonly IEnumerable<IJobSearchProvider> _providers = providers;

    public IJobSearchProvider? GetProvider(string? providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return null;
        }

        return _providers.FirstOrDefault(p =>
            p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<IJobSearchProvider> GetProviders(string? providerNames)
    {
        if (string.IsNullOrWhiteSpace(providerNames))
        {
            yield break;
        }

        var names = providerNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var name in names)
        {
            var provider = GetProvider(name);
            if (provider != null)
            {
                yield return provider;
            }
        }
    }
}