namespace WebCV.Web.Services;

public class ClientPersistenceService(IJSRuntime jsRuntime) : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _moduleTask = new(() =>
        jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/persistence.js").AsTask()
    );

    public async Task SaveDraftAsync<T>(string key, T data)
    {
        try
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("saveItem", key, data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving draft '{key}': {ex.Message}");
        }
    }

    public async Task<T?> GetDraftAsync<T>(string key)
    {
        try
        {
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<T?>("loadItem", key);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading draft '{key}': {ex.Message}");
            return default;
        }
    }

    public async Task ClearDraftAsync(string key)
    {
        try
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("clearItem", key);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing draft '{key}': {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            var module = await _moduleTask.Value;
            await module.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
