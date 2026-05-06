namespace AiCV.Web.Components.Shared;

public partial class AiModelPicker
{
    // ─── Parameters ──────────────────────────────────────────────────────────
    [Parameter, EditorRequired]
    public List<AIModelDto> AvailableModels { get; set; } = [];

    /// <summary>Two-way binding for the selected model ID.</summary>
    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Fired after a model is selected — use this for metadata lookup.</summary>
    [Parameter] public EventCallback<string?> AfterValueChanged { get; set; }

    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Clearable { get; set; } = true;
    [Parameter] public string BaseLabel { get; set; } = "Model";

    // ─── Internal state ───────────────────────────────────────────────────────
    private string? _costTypeFilter;

    // Keep an internal backing field in sync with Value parameter
    private string? _internalValue;
    protected override void OnParametersSet() => _internalValue = Value;

    // ─── Computed ─────────────────────────────────────────────────────────────
    private IEnumerable<IGrouping<string, AIModelDto>> ModelGroups =>
        AvailableModels
            .GroupBy(m => m.CostType ?? "Unknown")
            .OrderBy(g => g.Key == "Free" ? 0 : 1)
            .ThenBy(g => g.Key);

    private int FilteredCount => _costTypeFilter is null
        ? AvailableModels.Count
        : AvailableModels.Count(m => m.CostType == _costTypeFilter);

    private string Label => AvailableModels.Count == 0
        ? BaseLabel
        : _costTypeFilter is null
            ? $"{BaseLabel} ({AvailableModels.Count})"
            : $"{BaseLabel} ({FilteredCount} {_costTypeFilter})";

    // ─── Handlers ────────────────────────────────────────────────────────────
    private async Task HandleValueChanged(string? newValue)
    {
        _internalValue = newValue;
        await ValueChanged.InvokeAsync(newValue);
        await AfterValueChanged.InvokeAsync(newValue);
    }

    private void SetFilter(string? filter) => _costTypeFilter = filter;

    // ─── Color mapping ────────────────────────────────────────────────────────
    internal static Color GetCostTypeColor(string? costType) => costType switch
    {
        "Free"     => Color.Info,
        "Paid"     => Color.Warning,
        "Freemium" => Color.Tertiary,
        _          => Color.Default,
    };

    // ─── Search / filter ──────────────────────────────────────────────────────
    private Task<IEnumerable<string>> SearchModels(string value, CancellationToken _)
    {
        var pool = (IEnumerable<AIModelDto>)AvailableModels;
        if (_costTypeFilter is not null)
            pool = pool.Where(m => m.CostType == _costTypeFilter);

        var sorted = pool
            .OrderBy(m => m.CostType == "Free" ? 0 : 1)
            .ThenBy(m => m.ModelId);

        if (string.IsNullOrWhiteSpace(value))
            return Task.FromResult(sorted.Select(m => m.ModelId).AsEnumerable());

        return Task.FromResult(sorted
            .Where(m =>
                m.ModelId.Contains(value, StringComparison.OrdinalIgnoreCase)
                || (m.Name?.Contains(value, StringComparison.OrdinalIgnoreCase) == true))
            .Select(m => m.ModelId));
    }
}
