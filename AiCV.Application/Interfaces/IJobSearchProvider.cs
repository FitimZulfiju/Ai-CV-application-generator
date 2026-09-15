namespace AiCV.Application.Interfaces;

public interface IJobSearchProvider
{
    string ProviderName { get; }

    Task<List<JobSearchResult>> SearchAsync(
        JobSearchQuery query,
        CancellationToken cancellationToken = default
    );

    Task<JobDetail?> GetDetailAsync(
        string jobId,
        CancellationToken cancellationToken = default
    );
}
