namespace AiCV.Application.Interfaces;

public interface IDailyJobApplicationService
{
    Task<AutomationRunResult> RunForUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    );

    Task<AutomationRunResult> RunOnceAsync(
        string userId,
        CancellationToken cancellationToken = default
    );
}
