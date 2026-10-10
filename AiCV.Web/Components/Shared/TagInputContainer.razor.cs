namespace AiCV.Web.Components.Shared;

public partial class TagInputContainer
{
    [Parameter]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public string Placeholder { get; set; } = string.Empty;

    [Parameter]
    public List<string> Tags { get; set; } = [];

    [Parameter]
    public EventCallback<List<string>> TagsChanged { get; set; }

    [Parameter]
    public Func<string, CancellationToken, Task<IEnumerable<string>>>? SearchFunc { get; set; }

    private string _searchText = string.Empty;
    private MudAutocomplete<string>? _autocomplete;

    private async Task<IEnumerable<string>> SearchFuncWrapper(string value, CancellationToken ct)
    {
        if (SearchFunc == null) return [];
        var results = await SearchFunc(value, ct);
        // Exclude already selected tags
        return results.Where(r => !Tags.Contains(r, StringComparer.InvariantCultureIgnoreCase));
    }

    private void OnTextChanged(string text)
    {
        _searchText = text;
    }

    private async Task OnItemSelected(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !Tags.Contains(value, StringComparer.InvariantCultureIgnoreCase))
        {
            Tags.Add(value);
            await TagsChanged.InvokeAsync(Tags);
        }

        // Always clear text after selection
        _searchText = string.Empty;
        if (_autocomplete != null)
        {
            await _autocomplete.ClearAsync();
        }
        StateHasChanged();
    }

    private async Task OnKeyUp(KeyboardEventArgs e)
    {
        if ((e.Key == "Enter" || e.Key == ",") && !string.IsNullOrWhiteSpace(_searchText))
        {
            var newTag = _searchText.TrimEnd(',');
            if (!string.IsNullOrWhiteSpace(newTag) && !Tags.Contains(newTag, StringComparer.InvariantCultureIgnoreCase))
            {
                Tags.Add(newTag);
                await TagsChanged.InvokeAsync(Tags);
            }
            _searchText = string.Empty;
            if (_autocomplete != null)
            {
                await _autocomplete.ClearAsync();
            }
            StateHasChanged();
        }
    }

    private async Task RemoveTag(string tag)
    {
        if (Tags.Remove(tag))
        {
            await TagsChanged.InvokeAsync(Tags);
        }
    }
}

