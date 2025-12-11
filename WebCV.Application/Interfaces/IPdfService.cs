namespace WebCV.Application.Interfaces;

public interface IPdfService
{
    Task<byte[]> GenerateCvAsync(CandidateProfile profile);
    Task<byte[]> GenerateCoverLetterAsync(string letterContent, CandidateProfile profile, string jobTitle, string companyName);
}
