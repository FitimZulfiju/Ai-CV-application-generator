namespace WebCV.Infrastructure.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<User>(options)
    {
        public DbSet<CandidateProfile> CandidateProfiles { get; set; } = default!;
        public DbSet<Experience> Experiences { get; set; } = default!;
        public DbSet<Education> Educations { get; set; } = default!;
        public DbSet<Skill> Skills { get; set; } = default!;
        public DbSet<Project> Projects { get; set; } = default!;
        public DbSet<Language> Languages { get; set; } = default!;
        public DbSet<Interest> Interests { get; set; } = default!;
        public DbSet<JobPosting> JobPostings { get; set; } = default!;
        public DbSet<GeneratedApplication> GeneratedApplications { get; set; } = default!;
        public DbSet<UserSettings> UserSettings { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User -> CandidateProfile (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.CandidateProfile)
                .WithOne(p => p.User)
                .HasForeignKey<CandidateProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> UserSettings (One-to-One)
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserSettings)
                .WithOne(s => s.User)
                .HasForeignKey<UserSettings>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> GeneratedApplications (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.GeneratedApplications)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CandidateProfile>()
                .HasMany(p => p.WorkExperience)
                .WithOne(e => e.CandidateProfile)
                .HasForeignKey(e => e.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CandidateProfile>()
                .HasMany(p => p.Educations)
                .WithOne(e => e.CandidateProfile)
                .HasForeignKey(e => e.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CandidateProfile>()
                .HasMany(p => p.Skills)
                .WithOne(s => s.CandidateProfile)
                .HasForeignKey(s => s.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CandidateProfile>()
                .HasMany(p => p.Projects)
                .WithOne(pr => pr.CandidateProfile)
                .HasForeignKey(pr => pr.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CandidateProfile>()
                .HasMany(p => p.Languages)
                .WithOne(l => l.CandidateProfile)
                .HasForeignKey(l => l.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CandidateProfile>()
                .HasMany(p => p.Interests)
                .WithOne(i => i.CandidateProfile)
                .HasForeignKey(i => i.CandidateProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
