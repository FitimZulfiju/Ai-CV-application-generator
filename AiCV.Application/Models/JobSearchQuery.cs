namespace AiCV.Application.Models;

public record JobSearchQuery(
    string Query,
    string Provider = "Jobindex",
    string? Location = null,
    string? Region = null,
    int MaxResults = 20,
    int JobAgeDays = 7
);
