namespace AiCV.Web.Services;

public class ClientPersistenceService(IJSRuntime jsRuntime, ILogger<ClientPersistenceService> logger) : IAsyncDisposable
{
    private readonly ILogger<ClientPersistenceService> _logger = logger;
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
            _logger.LogError(ex, "Error saving draft '{Key}'", key);
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
            _logger.LogError(ex, "Error loading draft '{Key}'", key);
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
            _logger.LogError(ex, "Error clearing draft '{Key}'", key);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask.IsValueCreated)
        {
            try
            {
                var module = await _moduleTask.Value;
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit is disconnected, module is already disposed
            }
            catch (ObjectDisposedException)
            {
                // JS runtime is already disposed
            }
            catch (InvalidOperationException)
            {
                // JS interop calls cannot be made (prerendering or disconnected)
            }
        }

        GC.SuppressFinalize(this);
    }
}
