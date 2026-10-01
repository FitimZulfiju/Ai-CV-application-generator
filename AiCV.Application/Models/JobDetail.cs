namespace AiCV.Application.Models;

public class JobDetail
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime? DatePosted { get; set; }
    public DateTime? Deadline { get; set; }
    public string ApplyUrl { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
}
