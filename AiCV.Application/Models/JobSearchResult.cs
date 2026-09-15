namespace AiCV.Application.Models;

public record JobSearchResult(
    string Id,
    string Provider,
    string Title,
    string Company,
    string Location,
    DateTime? DatePosted,
    string Url,
    string ApplyUrl,
    string DescriptionSnippet
);
