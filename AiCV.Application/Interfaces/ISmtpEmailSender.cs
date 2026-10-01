namespace AiCV.Application.Interfaces;

public interface ISmtpEmailSender
{
    Task SendAutomationSummaryAsync(string to, string subject, string heading, string bodyHtml);
}