namespace AiCV.Infrastructure.Services.PdfTemplates;

public interface IPdfTemplateBuilder
{
    void ComposeHeader(IContainer container, CandidateProfile profile);
    void ComposePageOne(IContainer container, CandidateProfile profile, float fontSize);
    void ComposePageTwo(IContainer container, CandidateProfile profile, float fontSize);
    void ComposePageThree(IContainer container, CandidateProfile profile, float fontSize);
    void ComposeCoverLetter(
        IContainer container,
        string letterContent,
        CandidateProfile profile,
        float fontSize
    );
    int GetPageCount(byte[] pdfBytes);
}
