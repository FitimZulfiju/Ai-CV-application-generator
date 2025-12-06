namespace WebCV.Web.Components.Shared;

public partial class CvPreview
{
    [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
    
    [Parameter] public CandidateProfile? Profile { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Profile != null)
        {
             // Small delay to ensure DOM is fully calculated
            await Task.Delay(100);
            await JSRuntime.InvokeVoidAsync("cvScaler.fitContentToPages");
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private static string FormatSummary(string? summary)
    {
        if (string.IsNullOrEmpty(summary)) return string.Empty;
        // Simple bold formatting for Markdown-like syntax
        return summary.Replace("**", "<strong>").Replace("</strong>", "</strong>")
                      .Replace("\n", "<br>");
    }

    private static string FormatDescription(string? description)
    {
        if (string.IsNullOrEmpty(description)) return string.Empty;
        
        // If description is already HTML (li tags), return as is
        if (description.Contains("<li>")) return description;

        // Otherwise, split by newlines and wrap in li
        var lines = description.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var sb = new System.Text.StringBuilder();
        foreach (var line in lines)
        {
            var cleanLine = line.Trim().TrimStart('-', '*').Trim();
            if (!string.IsNullOrEmpty(cleanLine))
            {
                // Handle bold formatting
                cleanLine = cleanLine.Replace("**", "<strong>").Replace("</strong>", "</strong>");
                sb.AppendLine($"<li>{cleanLine}</li>");
            }
        }
        return sb.ToString();
    }

    private static string CalculateDuration(DateTime? start, DateTime? end)
    {
        if (!start.HasValue) return "";
        
        var endDate = end ?? DateTime.Now;
        var totalMonths = ((endDate.Year - start.Value.Year) * 12) + endDate.Month - start.Value.Month + 1;

        var years = totalMonths / 12;
        var months = totalMonths % 12;

        var parts = new List<string>();
        if (years > 0) parts.Add($"{years} year{(years > 1 ? "s" : "")}");
        if (months > 0) parts.Add($"{months} month{(months > 1 ? "s" : "")}");
        
        return string.Join(" ", parts);
    }
}
