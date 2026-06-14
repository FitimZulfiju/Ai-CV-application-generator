namespace AiCV.Application.Common;

public static class CvHelpers
{
    public static string CalculateDuration(
        DateTime? start,
        DateTime? end,
        bool isCurrentRole,
        IStringLocalizer localizer
    )
    {
        if (!start.HasValue || isCurrentRole)
            return "";

        var endDate = end ?? DateTime.Now;
        var totalMonths = ((endDate.Year - start.Value.Year) * 12) + endDate.Month - start.Value.Month + 1;
        
        // If the start and end month are the same but in different years, 
        // CVs usually treat this as exactly N years (e.g. July 2023 to July 2024 = 12 months)
        if (endDate.Month == start.Value.Month && endDate.Year > start.Value.Year)
        {
            totalMonths -= 1;
        }

        if (totalMonths <= 0) 
        {
            totalMonths = 1;
        }

        var years = totalMonths / 12;
        var months = totalMonths % 12;

        var parts = new List<string>();
        if (years > 0)
        {
            var yearKey = years > 1 ? "Years" : "Year";
            parts.Add($"{years} {localizer[yearKey]}");
        }
        if (months > 0)
        {
            var monthKey = months > 1 ? "Months" : "Month";
            parts.Add($"{months} {localizer[monthKey]}");
        }

        return string.Join(" ", parts);
    }
}
