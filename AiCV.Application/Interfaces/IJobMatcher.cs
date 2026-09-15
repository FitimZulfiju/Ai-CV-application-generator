namespace AiCV.Application.Interfaces;

public interface IJobMatcher
{
    Task<List<MatchedJob>> MatchAsync(
        CandidateProfile profile,
        List<JobSearchResult> jobs,
        int topN = 10,
        CancellationToken cancellationToken = default
    );
}
