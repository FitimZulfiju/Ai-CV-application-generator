namespace AiCV.Application.Models;

public record AutomationRunResult(
    bool Success,
    int JobsFound,
    int JobsMatched,
    int ApplicationsGenerated,
    List<string> Errors,
    DateTime CompletedAt
);
