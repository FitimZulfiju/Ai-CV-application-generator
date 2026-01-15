namespace AiCV.Application.Interfaces;

public interface IJobPostScraper
{
    Task<JobPosting> ScrapeJobPostingAsync(string url);
}
