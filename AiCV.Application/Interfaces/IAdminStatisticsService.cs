namespace AiCV.Application.Interfaces;

/// <summary>
/// Service interface for admin statistics
/// </summary>
public interface IAdminStatisticsService
{
    Task<AdminStatisticsDto> GetStatisticsAsync();

    /// <summary>
    /// Generates a CSV file of application statistics
    /// </summary>
    /// <returns>Byte array of CSV content</returns>
    Task<byte[]> GetStatisticsCsvAsync();
}
