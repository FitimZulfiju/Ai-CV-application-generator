namespace WebCV.Infrastructure.Services;

public class PdfService(IWebHostEnvironment env) : IPdfService
{
    private readonly IWebHostEnvironment _env = env;

    // Define colors
    private static readonly string PrimaryColor = "#2563eb";
    private static readonly string TextColor = "#000000";
    private static readonly string SecondaryTextColor = "#6b7280";

    public Task<byte[]> GenerateCvAsync(CandidateProfile profile)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial).FontColor(TextColor));

                page.Header().ShowOnce().Element(c => ComposeHeader(c, profile));
                page.Content().Element(c => ComposeContent(c, profile));
                
                page.Footer().AlignCenter().Text(x => {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    public Task<byte[]> GenerateCoverLetterAsync(string letterContent, CandidateProfile profile, string jobTitle, string companyName)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial).FontColor(TextColor));
                
                page.Content().Column(col => {
                    // Header (Contact Info)
                    col.Item().Text(profile.FullName).FontSize(16).Bold().FontColor(PrimaryColor);
                    col.Item().PaddingTop(0.2f, Unit.Centimetre);
                    col.Item().Text($"{profile.Email} | {profile.PhoneNumber}").FontSize(10).FontColor(SecondaryTextColor);
                    if (!string.IsNullOrEmpty(profile.Location))
                    {
                        col.Item().Text(profile.Location).FontSize(10).FontColor(SecondaryTextColor);
                    }
                    col.Item().PaddingBottom(1, Unit.Centimetre);

                    // Date
                    col.Item().Text(DateTime.Now.ToString("MMMM dd, yyyy"));
                    col.Item().PaddingBottom(1, Unit.Centimetre);

                    // Subject
                    col.Item().Text($"RE: {jobTitle} Application - {companyName}").Bold();
                    col.Item().PaddingBottom(0.5f, Unit.Centimetre);
                    
                    // Main content
                    if (!string.IsNullOrWhiteSpace(letterContent))
                    {
                        foreach(var paragraph in letterContent.Split(["\n", "\r\n"], StringSplitOptions.RemoveEmptyEntries)) 
                        {
                             col.Item().Text(paragraph.Trim());
                             col.Item().PaddingBottom(0.3f, Unit.Centimetre);
                        }
                    }
                    else 
                    {
                         col.Item().Text("No content provided.").Italic();
                    }

                    // Sign-off
                    col.Item().PaddingTop(1, Unit.Centimetre);
                    col.Item().Text("Sincerely,");
                    col.Item().PaddingTop(0.5f, Unit.Centimetre);
                    col.Item().Text(profile.FullName);
                });
                
                page.Footer().AlignCenter().Text(x => x.CurrentPageNumber());
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    private void ComposeHeader(IContainer container, CandidateProfile profile)
    {
        container.Row(row =>
        {
            // Left Side: Name and Info
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(profile.FullName).FontSize(24).Bold().FontColor(PrimaryColor);
                col.Item().Text(profile.Title ?? "Candidate").FontSize(14).FontColor(SecondaryTextColor);
                
                col.Item().PaddingTop(0.5f, Unit.Centimetre);
                
                // Contact Info Grid
                col.Item().Table(table => {
                    table.ColumnsDefinition(columns => {
                        columns.ConstantColumn(20);
                        columns.RelativeColumn();
                    });

                    if (!string.IsNullOrEmpty(profile.Email))
                    {
                        table.Cell().Text("✉").FontFamily("Arial"); 
                        table.Cell().Text(profile.Email).FontSize(9);
                    }
                     if (!string.IsNullOrEmpty(profile.PhoneNumber))
                    {
                        table.Cell().Text("📞").FontFamily("Arial");
                        table.Cell().Text(profile.PhoneNumber).FontSize(9);
                    }
                     if (!string.IsNullOrEmpty(profile.Location))
                    {
                        table.Cell().Text("📍").FontFamily("Arial");
                        table.Cell().Text(profile.Location).FontSize(9);
                    }
                     if (!string.IsNullOrEmpty(profile.LinkedInUrl))
                    {
                        table.Cell().Text("🔗").FontFamily("Arial");
                        table.Cell().Text(profile.LinkedInUrl).FontSize(9);
                    }
                     if (!string.IsNullOrEmpty(profile.PortfolioUrl))
                    {
                        table.Cell().Text("🌐").FontFamily("Arial");
                        table.Cell().Text(profile.PortfolioUrl).FontSize(9);
                    }
                });
            });

            // Right Side: Photo
            if (!string.IsNullOrEmpty(profile.ProfilePictureUrl) && profile.ShowProfilePicture)
            {
                string path = Path.Combine(_env.WebRootPath, profile.ProfilePictureUrl.TrimStart('/', '\\'));
                if (File.Exists(path))
                {
                    // Use FitWidth (or just Image) because FitArea usually requires bounded Height, 
                    // and Row height is content-dependent.
                    row.ConstantItem(3, Unit.Centimetre).PaddingLeft(0.5f, Unit.Centimetre).Image(path);
                }
            }
        });
    }

    private static void ComposeContent(IContainer container, CandidateProfile profile)
    {
        container.Column(col =>
        {
            col.Item().PaddingTop(1, Unit.Centimetre);

            // Summary
            if (!string.IsNullOrWhiteSpace(profile.ProfessionalSummary))
            {
                SectionTitle(col, "Professional Summary");
                col.Item().Text(StripHtml(profile.ProfessionalSummary));
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }

            // Experience
            if (profile.WorkExperience != null && profile.WorkExperience.Count != 0)
            {
                 SectionTitle(col, "Work Experience");
                 
                 col.Item().Table(table => {
                     table.ColumnsDefinition(columns => {
                         columns.RelativeColumn();
                         columns.ConstantColumn(120);
                     });

                     foreach(var exp in profile.WorkExperience.OrderByDescending(e => e.StartDate))
                     {
                         // Row 1: Title & Date
                         table.Cell().Text(StripHtml(exp.JobTitle ?? "")).Bold().FontSize(11);
                         table.Cell().AlignRight().Text($"{exp.StartDate:MMM yyyy} - {(exp.EndDate.HasValue ? exp.EndDate.Value.ToString("MMM yyyy") : "Present")}").FontSize(10);
                         
                         // Row 2: Company
                         table.Cell().ColumnSpan(2).Text(StripHtml(exp.CompanyName ?? "")).FontSize(10).Italic();

                         // Row 3: Description
                         if (!string.IsNullOrWhiteSpace(exp.Description))
                         {
                             table.Cell().ColumnSpan(2).PaddingTop(0.2f, Unit.Centimetre).Text(StripHtml(exp.Description)).FontSize(9);
                         }
                         
                         // Spacer Row
                         table.Cell().ColumnSpan(2).PaddingBottom(0.5f, Unit.Centimetre);
                     }
                 });
            }

            // Education
            if (profile.Educations != null && profile.Educations.Count != 0)
            {
                SectionTitle(col, "Education");
                
                col.Item().Table(table => {
                     table.ColumnsDefinition(columns => {
                         columns.RelativeColumn();
                         columns.ConstantColumn(120);
                     });

                     foreach(var edu in profile.Educations.OrderByDescending(e => e.StartDate))
                     {
                         // Row 1: Degree & Date
                         table.Cell().Text(StripHtml(edu.Degree ?? "")).Bold().FontSize(11);
                         table.Cell().AlignRight().Text($"{edu.StartDate:yyyy} - {(edu.EndDate.HasValue ? edu.EndDate.Value.ToString("yyyy") : "Present")}").FontSize(10);
                         
                         // Row 2: Institution
                         table.Cell().ColumnSpan(2).Text(StripHtml(edu.InstitutionName ?? "")).FontSize(10).Italic();
                         
                         // Spacer
                         table.Cell().ColumnSpan(2).PaddingBottom(0.3f, Unit.Centimetre);
                     }
                 });
                 col.Item().PaddingBottom(0.5f, Unit.Centimetre);
            }

            // Skills
            if (profile.Skills != null && profile.Skills.Count != 0)
            {
                SectionTitle(col, "Skills");
                
                var categories = profile.Skills.GroupBy(s => s.Category ?? "Other").ToList();
                foreach(var cat in categories)
                {
                    col.Item().Text(t => {
                        t.Span($"{StripHtml(cat.Key)}: ").Bold().FontSize(10);
                        t.Span(string.Join(", ", cat.Select(s => StripHtml(s.Name)).Distinct()));
                    });
                    col.Item().PaddingBottom(0.2f, Unit.Centimetre);
                }
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }
            
            // Projects
            if (profile.Projects != null && profile.Projects.Count != 0)
            {
                SectionTitle(col, "Projects");
                
                col.Item().Table(table => {
                     table.ColumnsDefinition(columns => {
                         columns.RelativeColumn();
                         columns.ConstantColumn(120);
                     });

                    foreach(var proj in profile.Projects.OrderByDescending(p => p.StartDate))
                    {
                        // Row 1: Name & Date
                        table.Cell().Text(StripHtml(proj.Name ?? "")).Bold().FontSize(11);
                        
                        string dateStr = "";
                        if (proj.StartDate.HasValue)
                             dateStr = $"{proj.StartDate:MMM yyyy} - {(proj.EndDate.HasValue ? proj.EndDate.Value.ToString("MMM yyyy") : "Present")}";
                        table.Cell().AlignRight().Text(dateStr).FontSize(10);
                        
                        // Row 2: Role
                        if (!string.IsNullOrEmpty(proj.Role))
                             table.Cell().ColumnSpan(2).Text(StripHtml(proj.Role)).FontSize(10).Italic();
                        else 
                             table.Cell().ColumnSpan(2); // Empty cell placeholder if needed to keep grid strict? No, span 2 is fine

                        // Row 3: Description
                        if (!string.IsNullOrEmpty(proj.Description))
                            table.Cell().ColumnSpan(2).PaddingTop(0.1f, Unit.Centimetre).Text(StripHtml(proj.Description)).FontSize(9);
                            
                        // Spacer
                        table.Cell().ColumnSpan(2).PaddingBottom(0.5f, Unit.Centimetre);
                    }
                });
            }
            
            // Languages
             if (profile.Languages != null && profile.Languages.Count != 0)
            {
                SectionTitle(col, "Languages");
                col.Item().Text(string.Join(" | ", profile.Languages.Select(l => $"{StripHtml(l.Name)} ({StripHtml(l.Proficiency)})")));
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }
            
             if (profile.Interests != null && profile.Interests.Count != 0)
            {
                SectionTitle(col, "Interests");
                col.Item().Text(string.Join(", ", profile.Interests.Select(i => StripHtml(i.Name))));
            }
        });
    }

    private static void SectionTitle(ColumnDescriptor column, string title)
    {
         column.Item().PaddingBottom(0.3f, Unit.Centimetre)
               .BorderBottom(1).BorderColor(PrimaryColor)
               .Text(title.ToUpper()).FontSize(12).Bold().FontColor(PrimaryColor).LetterSpacing(0.05f);
    }

    private static string StripHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);
    }
}
