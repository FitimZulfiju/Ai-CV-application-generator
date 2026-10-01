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
}