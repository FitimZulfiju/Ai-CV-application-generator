namespace WebCV.Infrastructure.Services
{
    public class CVService : ICVService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public CVService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<CandidateProfile> GetProfileAsync(string userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var profile = await context.CandidateProfiles
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.WorkExperience)
                .Include(p => p.Educations)
                .Include(p => p.Skills)
                .Include(p => p.Projects)
                .Include(p => p.Languages)
                .Include(p => p.Interests)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                var userExists = await context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    return null!;
                }
                profile = new CandidateProfile { UserId = userId };
                context.CandidateProfiles.Add(profile);
                await context.SaveChangesAsync();
            }

            return profile;
        }

        public async Task SaveProfileAsync(CandidateProfile profile)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            if (profile.Id == 0)
            {
                context.CandidateProfiles.Add(profile);
            }
            else
            {
                context.CandidateProfiles.Update(profile);
            }

            await context.SaveChangesAsync();
        }

        public async Task UpdateProfilePictureAsync(int profileId, string imageUrl)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            await context.CandidateProfiles
                .Where(p => p.Id == profileId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.ProfilePictureUrl, imageUrl));
        }

        public async Task<List<GeneratedApplication>> GetApplicationsAsync(string userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.GeneratedApplications
                .Include(a => a.JobPosting)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();
        }

        public async Task<GeneratedApplication?> GetApplicationAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.GeneratedApplications
                .Include(a => a.JobPosting)
                .Include(a => a.CandidateProfile)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task SaveApplicationAsync(GeneratedApplication application)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            if (application.Id == 0)
            {
                context.GeneratedApplications.Add(application);
            }
            else
            {
                context.GeneratedApplications.Update(application);
            }

            await context.SaveChangesAsync();
        }

        public async Task DeleteApplicationAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var app = await context.GeneratedApplications.FindAsync(id);
            if (app != null)
            {
                context.GeneratedApplications.Remove(app);
                await context.SaveChangesAsync();
            }
        }
    }
}
