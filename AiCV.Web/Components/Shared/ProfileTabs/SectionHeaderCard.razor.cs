namespace AiCV.Web.Components.Shared.ProfileTabs;

public partial class SectionHeaderCard
{
    [Parameter]
    public SectionConfig Section { get; set; } = new();

    [Parameter]
    public string DefaultNameKey { get; set; } = string.Empty;
}
