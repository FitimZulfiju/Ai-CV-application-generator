namespace AiCV.Tests.Services;

public class AIPromptBuilderTests
{
    [Fact]
    public void Build_IsResumeTrue_GeneratesJsonPromptWithCorrectFields()
    {
        // Arrange
        var profile = new CandidateProfile
        {
            FullName = "John Doe",
            ProfessionalSummary = "A great developer.",
            Skills = new List<Skill> { new Skill { Name = "C#" }, new Skill { Name = "Blazor" } },
            WorkExperience = new List<Experience>
            {
                new Experience { JobTitle = "Dev", CompanyName = "TechCorp", StartDate = new DateTime(2020, 1, 1), IsCurrentRole = true, Description = "Did stuff" }
            }
        };

        var job = new JobPosting
        {
            Title = "Senior Developer",
            CompanyName = "MegaCorp",
            Url = "https://example.com/job",
            Description = "We need a senior dev."
        };

        // Act
        var result = AIPromptBuilder.Build(
            profile: profile,
            job: job,
            language: "English",
            dearHiringManager: "Dear Hiring Manager",
            sincerely: "Sincerely",
            isResume: true
        );

        // Assert
        Assert.Contains("CRITICAL: GENERATE ALL CONTENT IN THE FOLLOWING LANGUAGE: English", result);
        Assert.Contains("Job Title: Senior Developer", result);
        Assert.Contains("Professional Summary: A great developer.", result);
        Assert.Contains("Skills: C#, Blazor", result);
        Assert.Contains("Dev at TechCorp", result);
        Assert.Contains("Return the result as a valid JSON object", result);
        Assert.Contains("\"DetectedJobDetails\":", result);
        Assert.Contains("\"TailoredProfile\":", result);
    }

    [Fact]
    public void Build_IsResumeFalse_GeneratesPlainTextCoverLetterPrompt()
    {
        // Arrange
        var profile = new CandidateProfile
        {
            FullName = "Jane Doe",
            ProfessionalSummary = "Expert manager."
        };

        var job = new JobPosting
        {
            Title = "Project Manager",
            CompanyName = "Global Inc.",
            Url = "https://example.com/pm",
            Description = "Lead projects."
        };

        // Act
        var result = AIPromptBuilder.Build(
            profile: profile,
            job: job,
            language: "French",
            dearHiringManager: "Cher recruteur",
            sincerely: "Cordialement",
            isResume: false
        );

        // Assert
        Assert.Contains("CRITICAL: GENERATE ALL CONTENT IN THE FOLLOWING LANGUAGE: French", result);
        Assert.Contains("Return the result as PLAIN TEXT. Do NOT use JSON or Markdown code blocks.", result);
        Assert.Contains("Cher recruteur", result);
        Assert.Contains("Cordialement", result);
        Assert.Contains("Jane Doe", result);
        Assert.DoesNotContain("\"DetectedJobDetails\":", result);
    }

    [Fact]
    public void BuildEmailPrompt_FormatsCorrectly()
    {
        // Arrange
        var profile = new CandidateProfile { FullName = "Bob Smith" };
        var job = new JobPosting { Title = "DevOps", CompanyName = "CloudNet" };
        var coverLetter = "Here is my long cover letter. " + new string('x', 600);

        // Act
        var result = AIPromptBuilder.BuildEmailPrompt(
            profile: profile,
            job: job,
            language: "English",
            coverLetter: coverLetter,
            candidateNameLabel: "Name",
            positionLabel: "Pos",
            companyLabel: "Comp",
            summaryLabel: "Summary",
            writeInstruction: "Write a short email."
        );

        // Assert
        Assert.Contains("Name: Bob Smith", result);
        Assert.Contains("Pos: DevOps", result);
        Assert.Contains("Comp: CloudNet", result);
        Assert.Contains("Write a short email.", result);
        
        // Ensure cover letter is truncated to ~500 chars (plus formatting)
        Assert.Contains(coverLetter.Substring(0, 500) + "...", result);
        Assert.DoesNotContain(new string('x', 600), result);
    }
}
