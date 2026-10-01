namespace AiCV.Infrastructure.Services.Automation;

public class AutomationOptions
{
    public const string SectionName = "Automation";

    public string DefaultCron { get; set; } = "0 6 * * *";
    public int DefaultMaxApplicationsPerRun { get; set; } = 10;
    public int PollIntervalSeconds { get; set; } = 60;
    public JobindexOptions Jobindex { get; set; } = new();
}

public class JobindexOptions
{
    public string BaseUrl { get; set; } = "https://www.jobindex.dk/jobsoegning";
    public string SearchPageUrl { get; set; } = "https://www.jobindex.dk/jobsoegning";
    public double RateLimitPerSecond { get; set; } = 1;
}