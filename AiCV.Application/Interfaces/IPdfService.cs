namespace AiCV.Application.Interfaces;

public interface IPdfService
{
    Task<byte[]> GenerateCvAsync(CandidateProfile profile, CvTemplate template);
    Task<byte[]> GenerateCoverLetterAsync(
        string letterContent,
        CandidateProfile profile,
        string jobTitle,
        string companyName,
        CvTemplate template
    );
}
