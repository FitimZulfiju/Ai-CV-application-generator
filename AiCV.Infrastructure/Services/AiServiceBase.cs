namespace AiCV.Infrastructure.Services;

public abstract class AiServiceBase(IStringLocalizer<AicvResources> localizer) : IAIService
{
    protected readonly IStringLocalizer<AicvResources> _localizer = localizer;

    protected abstract AIProvider Provider { get; }

    /// <summary>
    /// Override this if the service uses HTTP for probing.
    /// </summary>
    protected virtual Task<HttpResponseMessage> SendProbeRequestAsync() =>
        throw new NotImplementedException("This service does not use HTTP for probing.");

    /// <summary>
    /// Override this if the service uses an SDK action for probing.
    /// </summary>
    protected virtual Task SendProbeActionAsync() =>
        throw new NotImplementedException("This service does not use SDK actions for probing.");

    /// <summary>
    /// Indicates if the service uses HTTP probing by default.
    /// </summary>
    protected virtual bool UseHttpProbing => true;

    public virtual async Task<TestAccessResult> TestAccessAsync()
    {
        if (UseHttpProbing)
        {
            return await ExecuteTestProbeAsync(Provider, SendProbeRequestAsync);
        }
        else
        {
            return await ExecuteTestProbeAsync(Provider, SendProbeActionAsync);
        }
    }

    #region Generation Methods (To be implemented by subclasses)

    public abstract Task<string> GenerateCoverLetterAsync(
        CandidateProfile profile,
        JobPosting job,
        string? customPrompt = null
    );

    public abstract Task<TailoredResumeResult> GenerateTailoredResumeAsync(
        CandidateProfile profile,
        JobPosting job,
        string? customPrompt = null
    );

    public abstract Task<string> GenerateApplicationEmailAsync(
        CandidateProfile profile,
        JobPosting job,
        string coverLetter,
        string? customPrompt = null
    );

    #endregion

    protected async Task<TestAccessResult> ExecuteTestProbeAsync(
        AIProvider provider,
        Func<Task<HttpResponseMessage>> sendRequest
    )
    {
        try
        {
            var response = await sendRequest();
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return new TestAccessResult
                {
                    Success = false,
                    Message = AIErrorMapper.MapError(
                        provider,
                        error,
                        response.StatusCode,
                        _localizer
                    ),
                    ErrorCode = response.StatusCode.ToString(),
                };
            }

            return new TestAccessResult
            {
                Success = true,
                Message = _localizer["AccessCheckSuccess"],
            };
        }
        catch (Exception ex)
        {
            return new TestAccessResult { Success = false, Message = ex.Message };
        }
    }

    protected async Task<TestAccessResult> ExecuteTestProbeAsync(
        AIProvider provider,
        Func<Task> probeAction
    )
    {
        try
        {
            await probeAction();
            return new TestAccessResult
            {
                Success = true,
                Message = _localizer["AccessCheckSuccess"],
            };
        }
        catch (Azure.RequestFailedException ex)
        {
            return new TestAccessResult
            {
                Success = false,
                Message = AIErrorMapper.MapError(
                    provider,
                    ex.Message,
                    (HttpStatusCode)ex.Status,
                    _localizer
                ),
                ErrorCode = ex.Status.ToString(),
            };
        }
        catch (Exception ex)
        {
            return new TestAccessResult { Success = false, Message = ex.Message };
        }
    }

    protected static string BuildPrompt(
        CandidateProfile profile,
        JobPosting job,
        bool isResume = false
    )
    {
        return AIPromptBuilder.Build(profile, job, isResume);
    }
}
