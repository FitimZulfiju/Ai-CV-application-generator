namespace AiCV.Infrastructure.Services
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
            var profile = await context
                .CandidateProfiles.AsNoTracking()
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
                // Delete existing child collections that will be replaced
                // This prevents duplication when skills/experiences/etc are recreated with Id=0
                // Using RemoveRange instead of ExecuteDeleteAsync for in-memory database compatibility
                var existingSkills = await context
                    .Skills.Where(s => s.CandidateProfileId == profile.Id)
                    .ToListAsync();
                context.Skills.RemoveRange(existingSkills);

                var existingExperiences = await context
                    .Experiences.Where(e => e.CandidateProfileId == profile.Id)
                    .ToListAsync();
                context.Experiences.RemoveRange(existingExperiences);

                var existingEducations = await context
                    .Educations.Where(e => e.CandidateProfileId == profile.Id)
                    .ToListAsync();
                context.Educations.RemoveRange(existingEducations);

                var existingProjects = await context
                    .Projects.Where(p => p.CandidateProfileId == profile.Id)
                    .ToListAsync();
                context.Projects.RemoveRange(existingProjects);

                var existingLanguages = await context
                    .Languages.Where(l => l.CandidateProfileId == profile.Id)
                    .ToListAsync();
                context.Languages.RemoveRange(existingLanguages);

                var existingInterests = await context
                    .Interests.Where(i => i.CandidateProfileId == profile.Id)
                    .ToListAsync();
                context.Interests.RemoveRange(existingInterests);

                // Ensure all child entities have correct profile ID and are new (Id=0)
                foreach (var skill in profile.Skills)
                {
                    skill.Id = 0;
                    skill.CandidateProfileId = profile.Id;
                }
                foreach (var exp in profile.WorkExperience)
                {
                    exp.Id = 0;
                    exp.CandidateProfileId = profile.Id;
                }
                foreach (var edu in profile.Educations)
                {
                    edu.Id = 0;
                    edu.CandidateProfileId = profile.Id;
                }
                foreach (var proj in profile.Projects)
                {
                    proj.Id = 0;
                    proj.CandidateProfileId = profile.Id;
                }
                foreach (var lang in profile.Languages)
                {
                    lang.Id = 0;
                    lang.CandidateProfileId = profile.Id;
                }
                foreach (var interest in profile.Interests)
                {
                    interest.Id = 0;
                    interest.CandidateProfileId = profile.Id;
                }

                context.CandidateProfiles.Update(profile);
            }

            await context.SaveChangesAsync();
        }

        public async Task UpdateProfilePictureAsync(int profileId, string imageUrl)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            await context
                .CandidateProfiles.Where(p => p.Id == profileId)
                .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(p => p.ProfilePictureUrl, imageUrl)
                );
        }

        public async Task<List<GeneratedApplication>> GetApplicationsAsync(string userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context
                .GeneratedApplications.Include(a => a.JobPosting)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();
        }

        public async Task<GeneratedApplication?> GetApplicationAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context
                .GeneratedApplications.Include(a => a.JobPosting)
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
