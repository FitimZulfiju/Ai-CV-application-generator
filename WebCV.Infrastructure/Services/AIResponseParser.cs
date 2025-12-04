namespace WebCV.Infrastructure.Services
{
    public static class AIResponseParser
    {
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public static TailoredResumeResult ParseTailoredResume(string jsonResponse, CandidateProfile originalProfile)
        {
            try
            {
                // Clean up JSON markdown code blocks if present (common in AI responses)
                var cleanJson = jsonResponse.Replace("```json", "").Replace("```", "").Trim();

                var resultDto = JsonSerializer.Deserialize<TailoredResumeResponseDto>(cleanJson, _jsonOptions);
                var tailoredProfileDto = resultDto?.TailoredProfile;

                var result = new TailoredResumeResult
                {
                    DetectedCompanyName = resultDto?.DetectedJobDetails?.CompanyName,
                    DetectedJobTitle = resultDto?.DetectedJobDetails?.JobTitle
                };

                if (tailoredProfileDto != null)
                {
                    var tailoredProfile = new CandidateProfile
                    {
                        Id = originalProfile.Id,
                        // Always use original PII as it's not requested from AI anymore
                        FullName = originalProfile.FullName,
                        Email = originalProfile.Email,
                        PhoneNumber = originalProfile.PhoneNumber,
                        LinkedInUrl = originalProfile.LinkedInUrl,
                        PortfolioUrl = originalProfile.PortfolioUrl,
                        Location = originalProfile.Location,
                        
                        // Use AI generated content if available, otherwise fallback
                        Title = !string.IsNullOrEmpty(tailoredProfileDto.Title) ? tailoredProfileDto.Title : originalProfile.Title,
                        
                        // Copy original Professional Summary (AI no longer rewrites this)
                        ProfessionalSummary = originalProfile.ProfessionalSummary
                    };

                    // Copy original Work Experience (AI no longer rewrites this)
                    if (originalProfile.WorkExperience != null)
                    {
                        tailoredProfile.WorkExperience = [.. originalProfile.WorkExperience];
                    }

                    // Map Skills with Categories
                    if (tailoredProfileDto.Skills != null)
                    {
                        var skillList = new List<Skill>();
                        foreach (var skillGroup in tailoredProfileDto.Skills)
                        {
                            if (skillGroup.Names != null)
                            {
                                skillList.AddRange(skillGroup.Names.Select(name => new Skill 
                                { 
                                    Name = name, 
                                    Category = !string.IsNullOrWhiteSpace(skillGroup.Category) ? skillGroup.Category : "General" 
                                }));
                            }
                        }
                        tailoredProfile.Skills = skillList;
                    }

                    // Copy static sections from original profile (Education, Projects, Languages, Interests)
                    // These are rarely tailored by AI, so we preserve the original data to avoid data loss
                    if (originalProfile.Educations != null)
                    {
                        tailoredProfile.Educations = [.. originalProfile.Educations];
                    }

                    if (originalProfile.Projects != null)
                    {
                        tailoredProfile.Projects = [.. originalProfile.Projects];
                    }

                    if (originalProfile.Languages != null)
                    {
                        tailoredProfile.Languages = [.. originalProfile.Languages];
                    }

                    if (originalProfile.Interests != null)
                    {
                        tailoredProfile.Interests = [.. originalProfile.Interests];
                    }

                    // Copy static sections from original profile (Projects, Languages, Interests)
                    // These are rarely tailored by AI, so we preserve the original data
                    if (originalProfile.Projects != null)
                    {
                        tailoredProfile.Projects = [.. originalProfile.Projects];
                    }

                    if (originalProfile.Languages != null)
                    {
                        tailoredProfile.Languages = [.. originalProfile.Languages];
                    }

                    if (originalProfile.Interests != null)
                    {
                        tailoredProfile.Interests = [.. originalProfile.Interests];
                    }

                    result.Profile = tailoredProfile;
                }
                else
                {
                    result.Profile = originalProfile;
                }

                return result;
            }
            catch (JsonException ex)
            {
                throw new Exception($"Failed to parse AI JSON response: {ex.Message}. Response was: {jsonResponse}");
            }
        }

        private class TailoredResumeResponseDto
        {
            public DetectedJobDetailsDto? DetectedJobDetails { get; set; }
            public CandidateProfileDto? TailoredProfile { get; set; }
        }

        private class DetectedJobDetailsDto
        {
            public string? CompanyName { get; set; }
            public string? JobTitle { get; set; }
        }

        private static DateTime? ParseDate(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString) || string.Equals(dateString, "Present", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Try parsing various formats
            if (DateTime.TryParse(dateString, out var date))
            {
                return date;
            }
            
            // Try parsing "yyyy" format
            if (DateTime.TryParseExact(dateString, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var yearDate))
            {
                return yearDate;
            }

             // Try parsing "MM/yyyy" or "MM-yyyy"
            string[] formats = ["MM/yyyy", "MM-yyyy", "MMM yyyy", "yyyy-MM-dd"];
            if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var formattedDate))
            {
                return formattedDate;
            }

            return null;
        }

        private class CandidateProfileDto
        {
            public string FullName { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string LinkedInUrl { get; set; } = string.Empty;
            public string PortfolioUrl { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public string ProfessionalSummary { get; set; } = string.Empty;
            public List<SkillGroupDto>? Skills { get; set; }
            public List<ExperienceDto>? WorkExperience { get; set; }
            // Education is now copied from original, so we don't need it in DTO anymore
            // public List<EducationDto>? Educations { get; set; }
        }

        private class SkillGroupDto
        {
            public string Category { get; set; } = string.Empty;
            public List<string>? Names { get; set; }
        }

        private class ExperienceDto
        {
            public string JobTitle { get; set; } = string.Empty;
            public string CompanyName { get; set; } = string.Empty;
            public string StartDate { get; set; } = string.Empty;
            public string EndDate { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        private class EducationDto
        {
            public string InstitutionName { get; set; } = string.Empty;
            public string Degree { get; set; } = string.Empty;
            public string StartDate { get; set; } = string.Empty;
            public string EndDate { get; set; } = string.Empty;
        }
    }
}
