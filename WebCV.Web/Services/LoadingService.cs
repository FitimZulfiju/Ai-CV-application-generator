namespace WebCV.Web.Services;
public class LoadingService : ILoadingService
{
    public bool IsVisible { get; private set; }
    public int Percentage { get; private set; }
    public string Message { get; private set; } = string.Empty;

    public event Action? OnChange;

    public void Show(string message = "", int percentage = 0)
    {
        IsVisible = true;
        Message = message;
        Percentage = percentage;
        NotifyStateChanged();
    }

    public void Update(int percentage, string? message = null)
    {
        Percentage = percentage;
        if (message != null)
        {
            Message = message;
        }
        NotifyStateChanged();
    }

    public void Hide()
    {
        IsVisible = false;
        Percentage = 0;
        Message = string.Empty;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
