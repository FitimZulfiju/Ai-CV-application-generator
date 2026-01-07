namespace AiCV.Web.Components.Shared;

public partial class PrintPreviewModal
{
    private bool _isVisible;
    private string _iframeKey = Guid.NewGuid().ToString();

    public byte[]? PdfData { get; private set; }
    public string? DocumentType { get; private set; }
    public string? DocumentTitle { get; private set; }

    public async Task ShowAsync(byte[] pdfData, string documentType, string documentTitle)
    {
        // Clear old data first to prevent showing stale content
        PdfData = null;
        _isVisible = false;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

        // Generate new key to force iframe refresh
        _iframeKey = Guid.NewGuid().ToString();

        // Set new data
        PdfData = pdfData;
        DocumentType = documentType;
        DocumentTitle = documentTitle;
        _isVisible = true;
        await InvokeAsync(StateHasChanged);
    }

    private void Close()
    {
        _isVisible = false;
        PdfData = null;
        DocumentType = null;
        DocumentTitle = null;
        _iframeKey = Guid.NewGuid().ToString();
        StateHasChanged();
    }

    private async Task DownloadPdf()
    {
        if (PdfData == null || PdfData.Length == 0)
            return;

        var filename = $"{DocumentType}_{DocumentTitle?.Replace(" ", "_") ?? "DOC"}.pdf";
        var base64 = Convert.ToBase64String(PdfData);

        await JS.InvokeVoidAsync(
            "eval",
            $@"
(function() {{
const byteCharacters = atob('{base64}');
const byteNumbers = new Array(byteCharacters.length);
for (let i = 0; i < byteCharacters.length; i++) {{
byteNumbers[i] = byteCharacters.charCodeAt(i);
}}
const byteArray = new Uint8Array(byteNumbers);
const blob = new Blob([byteArray], {{ type: 'application/pdf' }});
const link = document.createElement('a');
link.href = URL.createObjectURL(blob);
link.download = '{filename}';
document.body.appendChild(link);
link.click();
document.body.removeChild(link);
}})();
"
        );
    }
}
