using QuestPDF.Helpers;
namespace WebCV.Infrastructure.Services;

public class PdfService(IWebHostEnvironment env) : IPdfService
{
    private readonly IWebHostEnvironment _env = env;

    // Define colors
    // Define colors from CSS
    private static readonly string PrimaryColor = "#2c7be5"; // var(--primary-color)
    private static readonly string PrimaryDark = "#1e5fae";  // var(--primary-dark)
    private static readonly string AccentColor = "#10b981";  // var(--accent-color)
    private static readonly string TextDark = "#1f2937";     // var(--text-dark)
    private static readonly string TextMedium = "#4b5563";   // var(--text-medium)
    private static readonly string BackgroundLight = "#f9fafb"; // var(--bg-light)
    private static readonly string BorderColor = "#e5e7eb";  // var(--border-color)

    public Task<byte[]> GenerateCvAsync(CandidateProfile profile)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.0f, Unit.Centimetre); // Reduced slightly to fit div-like content, header has padding
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial).FontColor(TextDark));

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
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial).FontColor(TextDark));
                
                page.Content().Column(col => {
                    // Header (Contact Info)
                    col.Item().Text(profile.FullName).FontSize(16).Bold().FontColor(PrimaryColor);
                    col.Item().PaddingTop(0.2f, Unit.Centimetre);
                    col.Item().Text($"{profile.Email} | {profile.PhoneNumber}").FontSize(10).FontColor(TextMedium);
                    if (!string.IsNullOrEmpty(profile.Location))
                    {
                        col.Item().Text(profile.Location).FontSize(10).FontColor(TextMedium);
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
        // Header from CSS: Background Gradient (Simulated with Primary), Color White, Centered, Padding
        container.Background(PrimaryColor).Padding(1, Unit.Centimetre) // approx 2.5em
            .Column(col => 
            {
                 // Photo
                 if (!string.IsNullOrEmpty(profile.ProfilePictureUrl) && profile.ShowProfilePicture)
                 {
                     string path = Path.Combine(_env.WebRootPath, profile.ProfilePictureUrl.TrimStart('/', '\\'));
                     if (File.Exists(path))
                     {
                         col.Item().AlignCenter().Width(3, Unit.Centimetre).Height(3, Unit.Centimetre)
                            .Image(path); // Circular crop hard in simple QuestPDF without helper, assumes square/standard
                         col.Item().Height(0.5f, Unit.Centimetre);
                     }
                 }

                 // Name
                 col.Item().AlignCenter().Text(profile.FullName).FontSize(24).Bold().FontColor("#ffffff");
                 
                 // Title
                 col.Item().AlignCenter().Text(profile.Title ?? "Candidate").FontSize(14).FontColor("#ffffff").FontColor(Colors.Grey.Lighten4);
                 
                 // Contact Info
                 col.Item().PaddingTop(0.5f, Unit.Centimetre).AlignCenter().Text(t => 
                 {
                     t.DefaultTextStyle(x => x.FontColor("#ffffff").FontSize(9));
                     var parts = new List<string>();
                     if(!string.IsNullOrEmpty(profile.Email)) parts.Add(profile.Email);
                     if(!string.IsNullOrEmpty(profile.PhoneNumber)) parts.Add(profile.PhoneNumber);
                     if(!string.IsNullOrEmpty(profile.Location)) parts.Add(profile.Location);
                     
                     t.Span(string.Join(" | ", parts));
                 });

                 // Links
                  col.Item().PaddingTop(0.2f, Unit.Centimetre).AlignCenter().Text(t => 
                 {
                     t.DefaultTextStyle(x => x.FontColor("#ffffff").FontSize(9));
                     var parts = new List<string>();
                     if(!string.IsNullOrEmpty(profile.LinkedInUrl)) parts.Add($"LinkedIn: {profile.LinkedInUrl}");
                     if(!string.IsNullOrEmpty(profile.PortfolioUrl)) parts.Add($"GitHub: {profile.PortfolioUrl}");
                     
                     t.Span(string.Join(" | ", parts));
                 });
                 
                 // Bottom Accent Line (simulated with BorderBottom logic or just handled by Container end)
                 // CSS has ::after with accent color height 0.25em. 
                 // We can add a line item.
                 col.Item().PaddingTop(0.5f, Unit.Centimetre).Height(4).Background(AccentColor);
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
                // CSS: .summary { background: var(--bg-light); border-left: 0.25em solid var(--primary-color); padding: ... }
                col.Item().Background(BackgroundLight).BorderLeft(4).BorderColor(PrimaryColor).Padding(10)
                   .Column(c => 
                   {
                        // Summary Text
                        c.Item().Text(StripHtml(profile.ProfessionalSummary)).FontColor(TextMedium).FontSize(10).LineHeight(1.5f);
                   });
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }

            // Skills (Moved to Page 1)
            if (profile.Skills != null && profile.Skills.Count != 0)
            {
                SectionTitle(col, "Core Competencies"); // HTML says "CORE COMPETENCIES" (Skills)
                
                var categories = profile.Skills.GroupBy(s => s.Category ?? "Other").ToList();
                foreach(var cat in categories)
                {
                    col.Item().PaddingBottom(0.2f, Unit.Centimetre).Background(BackgroundLight).BorderLeft(4).BorderColor(PrimaryColor).Padding(10)
                       .Column(c => 
                       {
                           c.Item().Text(StripHtml(cat.Key)).Bold().FontSize(10).FontColor(PrimaryDark);
                           c.Item().Text(string.Join(", ", cat.Select(s => StripHtml(s.Name)).Distinct())).FontSize(9).FontColor(TextMedium);
                       });
                }
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }

            // Page Break 1 (Start of Page 2: Work Experience)
            col.Item().PageBreak();

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
                         table.Cell().Text(StripHtml(exp.JobTitle ?? "")).Bold().FontSize(11).FontColor(TextDark);
                         table.Cell().AlignRight().Text($"{exp.StartDate:MMM yyyy} - {(exp.EndDate.HasValue ? exp.EndDate.Value.ToString("MMM yyyy") : "Present")}").FontSize(10).FontColor(TextMedium);
                         
                         // Row 2: Company (Primary Color)
                         var companyText = StripHtml(exp.CompanyName ?? "");
                         if (!string.IsNullOrEmpty(exp.Location)) companyText += $" - {StripHtml(exp.Location)}";
                         
                         table.Cell().ColumnSpan(2).Text(companyText).FontSize(10).FontColor(PrimaryColor).SemiBold(); 

                         // Row 3: Description with Custom Bullets (▸)
                         if (!string.IsNullOrWhiteSpace(exp.Description))
                         {
                             // CSS uses ▸ (U+25B8) for achievements
                             var desc = StripHtml(exp.Description)
                                         .Replace("• ", "▸ ") // Replace standard bullet if present
                                         .Replace("- ", "▸ "); // Replace dash if present
                             
                             table.Cell().ColumnSpan(2).PaddingTop(0.2f, Unit.Centimetre)
                                  .Text(desc).FontSize(9).FontColor(TextMedium).LineHeight(1.5f);
                         }
                         
                         // Spacer Row
                         table.Cell().ColumnSpan(2).BorderBottom(1).BorderColor(BorderColor).PaddingBottom(0.5f, Unit.Centimetre).PaddingTop(0.5f, Unit.Centimetre);
                     }
                 });
                 col.Item().PaddingBottom(1, Unit.Centimetre);
            }

            // Page Break 2 (Start of Page 3: Education, etc)
            col.Item().PageBreak();

            // Education
            if (profile.Educations != null && profile.Educations.Count != 0)
            {
                SectionTitle(col, "Education");
                
                col.Item().Table(table => {
                     table.ColumnsDefinition(columns => {
                         columns.RelativeColumn();
                     });

                     foreach(var edu in profile.Educations.OrderByDescending(e => e.StartDate))
                     {
                         table.Cell().PaddingBottom(0.5f, Unit.Centimetre).Element(cell => 
                         {
                             cell.Background(BackgroundLight).BorderLeft(4).BorderColor(AccentColor).Padding(10).Column(c => 
                             {
                                 c.Item().Row(r => 
                                 {
                                     r.RelativeItem().Text(StripHtml(edu.Degree ?? "")).Bold().FontSize(11).FontColor(TextDark);
                                     r.ConstantItem(100).AlignRight().Text($"{edu.StartDate:yyyy} - {(edu.EndDate.HasValue ? edu.EndDate.Value.ToString("yyyy") : "Present")}").FontSize(10).FontColor(TextMedium);
                                 });
                                 
                                 c.Item().Text(StripHtml(edu.InstitutionName ?? "")).FontSize(10).FontColor(PrimaryColor).SemiBold();
                             });
                         });
                     }
                 });
                 col.Item().PaddingBottom(0.5f, Unit.Centimetre);
            }

            // Projects
            if (profile.Projects != null && profile.Projects.Count != 0)
            {
                SectionTitle(col, "Personal Projects"); // HTML title
                
                 col.Item().Table(table => {
                     table.ColumnsDefinition(columns => {
                         columns.RelativeColumn();
                     });

                     foreach(var proj in profile.Projects.OrderByDescending(p => p.StartDate))
                     {
                         table.Cell().PaddingBottom(0.5f, Unit.Centimetre).Element(cell => 
                         {
                             cell.Background(BackgroundLight).BorderLeft(4).BorderColor(PrimaryColor).Padding(10).Column(c => 
                             {
                                 c.Item().Row(r => {
                                     r.RelativeItem().Text(StripHtml(proj.Name ?? "")).Bold().FontSize(11).FontColor(TextDark);
                                     
                                     string dateStr = "";
                                     if (proj.StartDate.HasValue)
                                          dateStr = $"{proj.StartDate:MMM yyyy} - {(proj.EndDate.HasValue ? proj.EndDate.Value.ToString("MMM yyyy") : "Present")}";
                                     r.ConstantItem(120).AlignRight().Text(dateStr).FontSize(10).FontColor(TextMedium);
                                 });

                                 if (!string.IsNullOrEmpty(proj.Link) || !string.IsNullOrEmpty(proj.Technologies))
                                 {
                                     c.Item().PaddingBottom(0.1f, Unit.Centimetre).Text(t => 
                                     {
                                         t.DefaultTextStyle(x => x.FontSize(9).FontColor(TextMedium));
                                         if (!string.IsNullOrEmpty(proj.Link))
                                         {
                                             t.Span("Link: ").Bold().FontColor(PrimaryDark);
                                             t.Span(proj.Link).FontColor(PrimaryColor); 
                                             t.Span(" | ");
                                         }
                                         if (!string.IsNullOrEmpty(proj.Technologies))
                                         {
                                              t.Span("Technologies: ").Bold().FontColor(PrimaryDark);
                                              t.Span(StripHtml(proj.Technologies));
                                         }
                                     });
                                 }

                                 if (!string.IsNullOrEmpty(proj.Role))
                                      c.Item().Text(StripHtml(proj.Role)).FontSize(10).FontColor(PrimaryColor).SemiBold(); 

                                 if (!string.IsNullOrEmpty(proj.Description))
                                 {
                                      // CSS uses ✓ (U+2713) for project features
                                      var desc = StripHtml(proj.Description)
                                                  .Replace("• ", "✓ ")
                                                  .Replace("- ", "✓ ");

                                      c.Item().PaddingTop(0.1f, Unit.Centimetre).Text(desc).FontSize(9).FontColor(TextMedium).LineHeight(1.5f);
                                 }
                             });
                         });
                    }
                });
            }
            
            // Languages
             if (profile.Languages != null && profile.Languages.Count != 0)
            {
                SectionTitle(col, "Languages");
                col.Item().Text(string.Join(" | ", profile.Languages.Select(l => $"{StripHtml(l.Name)} ({StripHtml(l.Proficiency)})"))).FontColor(TextMedium);
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }
            
             if (profile.Interests != null && profile.Interests.Count != 0)
            {
                SectionTitle(col, "Interests");
                
                // Tags Layout: Row with wrapped items
                col.Item().Element(element => 
                {
                    element.Wrap(wrap => 
                    {
                        wrap.Spacing(5);
                        wrap.RunSpacing(5);
                        
                        foreach(var interest in profile.Interests)
                        {
                            wrap.Item().Border(1).BorderColor(BorderColor).Background(BackgroundLight).PaddingHorizontal(10).PaddingVertical(3).BorderRadius(10)
                                   .Text(StripHtml(interest.Name)).FontSize(9).FontColor(TextMedium).SemiBold();
                        }
                    });
                });
            }
        });
    }

    private static void SectionTitle(ColumnDescriptor column, string title)
    {
         column.Item().PaddingBottom(0.3f, Unit.Centimetre)
               .BorderBottom(2).BorderColor(PrimaryColor) // CSS: border-bottom: 0.188em solid var(--primary-color)
               .Text(title.ToUpper()).FontSize(12).Bold().FontColor(PrimaryDark).LetterSpacing(0.06f);
    }

    private static string StripHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        // Pre-process list items to bullets
        var text = input.Replace("<li>", "• ").Replace("</li>", "\n").Replace("<ul>", "").Replace("</ul>", "");
        // Remove remaining tags
        return System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty).Trim();
    }
}
