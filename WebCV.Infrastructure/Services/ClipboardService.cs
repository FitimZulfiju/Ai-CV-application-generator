namespace WebCV.Infrastructure.Services
{
    public class ClipboardService(IJSRuntime jsRuntime) : IClipboardService
    {
        private readonly IJSRuntime _jsRuntime = jsRuntime;

        public async Task CopyToClipboardAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            await _jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
    }
}
