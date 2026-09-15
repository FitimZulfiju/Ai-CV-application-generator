namespace AiCV.Application.Models;

public record MatchedJob(
    JobSearchResult Job,
    int Score,
    List<string> MatchedSkills,
    List<string> MatchedKeywords
);
