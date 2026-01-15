namespace AiCV.Application.Interfaces;

public interface IClipboardService
{
    Task CopyToClipboardAsync(string text);
}
