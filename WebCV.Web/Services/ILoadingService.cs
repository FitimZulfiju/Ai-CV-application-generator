namespace WebCV.Web.Services;

public interface ILoadingService
{
    bool IsVisible { get; }
    int Percentage { get; }
    string Message { get; }
    event Action? OnChange;

    void Show(string message = "", int percentage = 0);
    void Update(int percentage, string? message = null);
    void Hide();
}
