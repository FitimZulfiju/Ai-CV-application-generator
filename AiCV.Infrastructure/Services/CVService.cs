namespace AiCV.Infrastructure.Services;

public class CVService(IDbContextFactory<ApplicationDbContext> contextFactory) : ICVService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;

    public async Task<CandidateProfile> GetProfileAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
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
        else
        {
            profile.Languages = [.. profile.Languages.OrderBy(l => l.Id)];
            profile.Interests = [.. profile.Interests.OrderBy(i => i.Id)];
        }

        return profile;
    }

    public async Task SaveProfileAsync(CandidateProfile profile)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        if (profile.Id == 0)
        {
            profile.User = null;
            context.CandidateProfiles.Add(profile);
        }
        else
        {
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

            profile.User = null;

            context.CandidateProfiles.Update(profile);
        }

        await context.SaveChangesAsync();
    }

    public async Task UpdateProfilePictureAsync(int profileId, string imageUrl)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await context
            .CandidateProfiles.Where(p => p.Id == profileId)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(p => p.ProfilePictureUrl, imageUrl)
            );
    }

    public async Task<List<GeneratedApplication>> GetApplicationsAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context
            .GeneratedApplications.Include(a => a.JobPosting)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();
    }

    public async Task<GeneratedApplication?> GetApplicationAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context
            .GeneratedApplications.Include(a => a.JobPosting)
            .Include(a => a.CandidateProfile)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task SaveApplicationAsync(GeneratedApplication application)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

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
        await using var context = await _contextFactory.CreateDbContextAsync();
        var app = await context.GeneratedApplications.FindAsync(id);
        if (app != null)
        {
            context.GeneratedApplications.Remove(app);
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteApplicationsAsync(IEnumerable<int> ids)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var apps = await context.GeneratedApplications.Where(a => ids.Contains(a.Id)).ToListAsync();
        if (apps.Count != 0)
        {
            context.GeneratedApplications.RemoveRange(apps);
            await context.SaveChangesAsync();
        }
    }
}
