namespace WebCV.Application.Interfaces
{
    public interface ICVService
    {
        Task<CandidateProfile> GetProfileAsync(string userId);
        Task SaveProfileAsync(CandidateProfile profile);
        Task UpdateProfilePictureAsync(int profileId, string imageUrl);
        
        Task<List<GeneratedApplication>> GetApplicationsAsync(string userId);
        Task<GeneratedApplication?> GetApplicationAsync(int id);
        Task SaveApplicationAsync(GeneratedApplication application);
        Task DeleteApplicationAsync(int id);
    }
}
