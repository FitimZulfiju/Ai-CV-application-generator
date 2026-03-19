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
        var totalMonths =
            ((endDate.Year - start.Value.Year) * 12) + endDate.Month - start.Value.Month + 1;

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
