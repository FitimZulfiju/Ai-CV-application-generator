namespace WebCV.Web.Components.Shared;

public partial class CoverLetterPreview
{
    [Parameter] public CandidateProfile? Profile { get; set; }
    [Parameter] public string LetterContent { get; set; } = string.Empty;

    private string FormatLetter(string content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        // Basic formatting if needed, but pre-wrap handles most
        return content;
    }
}
