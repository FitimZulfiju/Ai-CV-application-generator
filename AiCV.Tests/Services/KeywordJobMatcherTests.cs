namespace AiCV.Tests.Services;

public class KeywordJobMatcherTests
{
    private readonly KeywordJobMatcher _matcher = new();

    private static CandidateProfile BuildProfile()
    {
        var profile = new CandidateProfile
        {
            Title = ".NET Backend Developer",
            ProfessionalSummary = "C# and Azure backend developer with SQL Server experience.",
            Location = "Copenhagen",
            Skills =
            [
                new Skill { Name = "C#" },
                new Skill { Name = "ASP.NET Core" },
                new Skill { Name = "SQL Server" },
            ],
            WorkExperience =
            [
                new Experience
                {
                    Description = "Built microservices with .NET and Docker on Azure.",
                },
            ],
        };
        return profile;
    }

    private static JobSearchResult Job(
        string id,
        string title,
        string location,
        string snippet,
        string company = "Acme",
        DateTime? posted = null) => new(
            Id: id,
            Provider: "Jobindex",
            Title: title,
            Company: company,
            Location: location,
            DatePosted: posted ?? DateTime.UtcNow.AddDays(-1),
            Url: $"https://www.jobindex.dk/vis-job/{id}",
            ApplyUrl: $"https://www.jobindex.dk/vis-job/{id}",
            DescriptionSnippet: snippet);

    [Fact]
    public async Task MatchAsync_ShouldRankSkillsAndTitle()
    {
        var profile = BuildProfile();
        var jobs = new List<JobSearchResult>
        {
            Job("1", ".NET Backend Developer", "København", "C#, ASP.NET Core, SQL Server, Azure"),
            Job("2", "Frontend Designer", "København", "Figma, UX design", company: "Other"),
        };

        var matches = await _matcher.MatchAsync(profile, jobs, topN: 10, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("1", matches[0].Job.Id);
        Assert.InRange(matches[0].Score, 0, 100);
        Assert.Contains("c#", matches[0].MatchedSkills, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Regression: naive substring matching scored a UX designer role as a match for a .NET
    /// profile, because the keyword "on" occurs inside "fr-on-tend".
    /// </summary>
    [Fact]
    public async Task MatchAsync_ShouldNotMatchUnrelatedJob()
    {
        var profile = BuildProfile();
        var jobs = new List<JobSearchResult>
        {
            Job("2", "Frontend Designer", "København", "Figma, UX design and prototyping", company: "Other"),
        };

        var matches = await _matcher.MatchAsync(profile, jobs, topN: 10, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(matches);
    }

    /// <summary>
    /// Regression: substring matching let short keywords match inside unrelated longer words
    /// ("go" in "Google", "java" in "JavaScript").
    /// </summary>
    [Theory]
    [InlineData("Golang Developer at Google", "Go, Google Cloud")]
    [InlineData("JavaScript Developer", "JavaScript, TypeScript, React")]
    public async Task MatchAsync_ShouldNotMatchOnPartialWords(string title, string snippet)
    {
        var profile = new CandidateProfile
        {
            Title = "Backend Developer",
            Location = "Copenhagen",
            Skills = [new Skill { Name = "Java" }, new Skill { Name = "Go" }],
        };

        var jobs = new List<JobSearchResult> { Job("1", title, "Berlin", snippet) };
        var matches = await _matcher.MatchAsync(profile, jobs, topN: 10, cancellationToken: TestContext.Current.CancellationToken);

        // "Go"/"Java" must match only as whole words, never inside Google/JavaScript.
        foreach (var match in matches)
        {
            Assert.DoesNotContain("javascript", match.MatchedSkills, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task MatchAsync_ShouldPreferCopenhagenOverOtherDanishCities()
    {
        var profile = BuildProfile();
        var jobs = new List<JobSearchResult>
        {
            Job("cph", ".NET Developer", "København", "C# and .NET"),
            Job("aarhus", ".NET Developer", "Aarhus", "C# and .NET", company: "Other"),
        };

        var matches = await _matcher.MatchAsync(profile, jobs, topN: 10, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("cph", matches[0].Job.Id);
        var cph = matches.First(m => m.Job.Id == "cph");
        var aarhus = matches.FirstOrDefault(m => m.Job.Id == "aarhus");
        if (aarhus is not null)
        {
            Assert.True(cph.Score > aarhus.Score, "Copenhagen job should outscore an out-of-region job.");
        }
    }

    [Theory]
    [InlineData("Lyngby")]
    [InlineData("Roskilde")]
    [InlineData("Ballerup")]
    [InlineData("Frederiksberg")]
    [InlineData("Storkøbenhavn")]
    public async Task MatchAsync_ShouldTreatGreaterCopenhagenAsInScope(string location)
    {
        var profile = BuildProfile();
        var jobs = new List<JobSearchResult>
        {
            Job("near", ".NET Developer", location, "C# and .NET"),
            Job("far", ".NET Developer", "Aalborg", "C# and .NET", company: "Other"),
        };

        var matches = await _matcher.MatchAsync(profile, jobs, topN: 10, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("near", matches[0].Job.Id);
    }

    [Fact]
    public async Task MatchAsync_ShouldPopulateMatchedSkills()
    {
        var profile = BuildProfile();
        var jobs = new List<JobSearchResult>
        {
            Job("1", "Backend Developer", "København", "We use C#, ASP.NET Core and SQL Server daily."),
        };

        var matches = await _matcher.MatchAsync(profile, jobs, topN: 10, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEmpty(matches[0].MatchedSkills);
        Assert.Contains("c#", matches[0].MatchedSkills, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MatchAsync_ShouldRespectTopN()
    {
        var profile = BuildProfile();
        var jobs = Enumerable.Range(1, 10)
            .Select(i => Job(i.ToString(), ".NET Developer", "København", "C# and .NET"))
            .ToList();

        var matches = await _matcher.MatchAsync(profile, jobs, topN: 3, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(3, matches.Count);
    }

    [Fact]
    public async Task MatchAsync_EmptyJobs_ReturnsEmpty()
    {
        var profile = BuildProfile();
        var matches = await _matcher.MatchAsync(profile, [], cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(matches);
    }

    [Theory]
    [InlineData("frontend", "on", false)]
    [InlineData("google", "go", false)]
    [InlineData("javascript", "java", false)]
    [InlineData("c# developer", "c#", true)]
    [InlineData("asp.net core role", "asp.net core", true)]
    [InlineData("we use .net daily", ".net", true)]
    [InlineData("sql server dba", "sql server", true)]
    public void ContainsWord_ShouldMatchOnWordBoundaries(string haystack, string term, bool expected)
    {
        Assert.Equal(expected, KeywordJobMatcher.ContainsWord(haystack, term));
    }
}
