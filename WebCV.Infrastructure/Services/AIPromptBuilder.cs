namespace WebCV.Infrastructure.Services
{
    public static class AIPromptBuilder
    {
        public static string Build(CandidateProfile profile, JobPosting job, bool isResume = false)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Job Title: {job.Title}");
            sb.AppendLine($"Company: {job.CompanyName}");
            sb.AppendLine($"Job URL: {job.Url}");
            sb.AppendLine($"Job Description: {job.Description}");
            sb.AppendLine($"Current Date: {DateTime.Now:MMMM dd, yyyy}");
            sb.AppendLine();

            sb.AppendLine("Candidate Profile:");
            // Exclude PII (Name, Email, Phone, Links, Location)
            sb.AppendLine($"Professional Summary: {profile.ProfessionalSummary}");

            if (profile.Skills != null && profile.Skills.Count != 0)
            {
                sb.AppendLine("Skills: " + string.Join(", ", profile.Skills.Select(s => s.Name)));
            }

            if (profile.WorkExperience != null && profile.WorkExperience.Count != 0)
            {
                sb.AppendLine("Work Experience:");
                foreach (var exp in profile.WorkExperience)
                {
                    sb.AppendLine($"- {exp.JobTitle} at {exp.CompanyName} ({exp.StartDate:MMM yyyy} - {(exp.IsCurrentRole ? "Present" : exp.EndDate?.ToString("MMM yyyy"))})");
                    sb.AppendLine($"  {exp.Description}");
                }
            }

            if (profile.Educations != null && profile.Educations.Count != 0)
            {
                sb.AppendLine("Education:");
                foreach (var edu in profile.Educations)
                {
                    sb.AppendLine($"- {edu.Degree} from {edu.InstitutionName} ({edu.StartDate:yyyy} - {edu.EndDate?.ToString("yyyy")})");
                }
            }

            sb.AppendLine();

            if (isResume)
            {
                sb.AppendLine("IMPORTANT: DO NOT USE EM-DASHES (—) IN THE JSON. ONLY USE HYPHENS (-).");
                sb.AppendLine("CRITICAL: Return the result as a valid JSON object matching the following structure.");
                sb.AppendLine("Do NOT include personal contact details (Name, Email, Phone, etc.) in the JSON. Only return the tailored content.");
                sb.AppendLine("IMPORTANT: Analyze the job description to extract the true Company Name and Job Title.");
                sb.AppendLine("Include ALL skills from the candidate's profile. Organize them into relevant categories for this job, placing the most important ones at the top.");
                sb.AppendLine("{");
                sb.AppendLine("  \"DetectedJobDetails\": { \"CompanyName\": \"...\", \"JobTitle\": \"...\" },");
                sb.AppendLine("  \"TailoredProfile\": { \"Title\": \"...\", \"Skills\": [ { \"Category\": \"...\", \"Names\": [\"...\"] } ] }");
                sb.AppendLine("}");
                sb.AppendLine("Ensure 'Description' fields use HTML <li> tags for bullet points.");
            }
            else
            {
                sb.AppendLine("IMPORTANT: Return the result as PLAIN TEXT. Do NOT use JSON or Markdown code blocks.");
                sb.AppendLine("Write a professional cover letter.");
                sb.AppendLine("TONE: Adopt a professional tone suitable for Danish/Scandinavian business culture: Direct, concise, humble but confident, and focused on the value the candidate brings to the company.");
                sb.AppendLine("CRITICAL INSTRUCTIONS:");
                sb.AppendLine("1. Do NOT include the CANDIDATE'S contact header (Name, Email, Phone). This is added automatically.");
                sb.AppendLine("2. DO include the CURRENT DATE (as provided above) and the COMPANY'S details at the top.");
                sb.AppendLine("3. Include a professional, concise SUBJECT line (e.g., 'RE: Application for [Job Title]'). Do NOT clutter the subject with the source.");
                sb.AppendLine("4. Start with a professional salutation (e.g., 'Dear Hiring Manager,' or 'Dear [Name],').");
                sb.AppendLine("5. Write the body of the letter. CRITICAL: In the VERY FIRST sentence, explicitly mention where the job was found based on the Job URL (e.g., '...as advertised on LinkedIn', '...on Indeed', or '...on your company website'). If no URL is provided, use '...as advertised'.");
                sb.AppendLine($"6. End with 'Sincerely,' followed by the candidate's name: {profile.FullName}.");
                sb.AppendLine("7. Do NOT use placeholders like '[Your Name]', '[Your Address]'.");
            }

            return sb.ToString();
        }
    }
}
