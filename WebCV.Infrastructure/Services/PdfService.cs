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
                page.Margin(1.25f, Unit.Centimetre); // Closer to CSS 2.5em/3.125em
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI").FontColor(TextDark));

                page.Header().ShowOnce().Element(c => ComposeHeader(c, profile));
                page.Content().Element(c => ComposeContent(c, profile));
                
                page.Footer().AlignCenter().Text(x => {
                    x.DefaultTextStyle(s => s.FontSize(7));
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
                page.Margin(1.25f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI").FontColor(TextDark));
                
                page.Header().ShowOnce().Element(c => ComposeHeader(c, profile));
                
                page.Content().Column(col => {
                    // Body with left border and background (matching .summary style)
                    col.Item().PaddingTop(0.8f, Unit.Centimetre);

                    // Wrap the entire letter content in a styled container (thinner: 1.5pt)
                    col.Item().Background(BackgroundLight).BorderLeft(1.5f).BorderColor(PrimaryColor)
                        .CornerRadius(5).Padding(10)
                        .Column(letterCol => 
                        {
                            // Date
                            letterCol.Item().Text(DateTime.Now.ToString("MMMM dd, yyyy")).FontSize(10);
                            letterCol.Item().PaddingBottom(0.8f, Unit.Centimetre);

                            // Subject
                            letterCol.Item().Text($"{jobTitle} Application - {companyName}").Bold().FontSize(11);
                            letterCol.Item().PaddingBottom(0.6f, Unit.Centimetre);
                            
                            // Main content
                            if (!string.IsNullOrWhiteSpace(letterContent))
                            {
                                foreach(var paragraph in letterContent.Split(["\n", "\r\n"], StringSplitOptions.RemoveEmptyEntries)) 
                                {
                                     letterCol.Item().Text(paragraph.Trim()).FontSize(10).LineHeight(1.5f);
                                     letterCol.Item().PaddingBottom(0.35f, Unit.Centimetre);
                                }
                            }
                            else 
                            {
                                 letterCol.Item().Text("No content provided.").Italic();
                            }

                            // Sign-off
                            letterCol.Item().PaddingTop(0.8f, Unit.Centimetre);
                            letterCol.Item().Text("Sincerely,").FontSize(10);
                            letterCol.Item().PaddingTop(0.5f, Unit.Centimetre);
                            letterCol.Item().Text(profile.FullName).FontSize(10);
                        });
                });
                
                // No footer for cover letter
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    private void ComposeHeader(IContainer container, CandidateProfile profile)
    {
        // Header from CSS: Background Gradient (Simulated with Primary), Color White, Centered, Padding
        container.Column(c => 
        {
            c.Item().Background(PrimaryColor).Padding(1, Unit.Centimetre) // approx 2.5em
                .Column(col => 
                {
                     // Photo
                     if (!string.IsNullOrEmpty(profile.ProfilePictureUrl) && profile.ShowProfilePicture)
                     {
                         string path = Path.Combine(_env.WebRootPath, profile.ProfilePictureUrl.TrimStart('/', '\\'));
                         if (File.Exists(path))
                         {
                             col.Item().AlignCenter().Width(3, Unit.Centimetre).Height(3, Unit.Centimetre)
                                .Image(path); 
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
                });

            // Bottom Accent Line - Full Width (thinner: 0.04cm)
            c.Item().Height(0.04f, Unit.Centimetre).Background(AccentColor);
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
                col.Item().Background(BackgroundLight).BorderLeft(1.5f).BorderColor(PrimaryColor).CornerRadius(5).Padding(10)
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
                SectionTitle(col, "Core Competencies");
                
                var categories = profile.Skills.GroupBy(s => s.Category ?? "Other").ToList();
                foreach(var cat in categories)
                {
                    col.Item().PaddingBottom(0.2f, Unit.Centimetre).Background(BackgroundLight).BorderLeft(1.5f).BorderColor(PrimaryColor).CornerRadius(5).Padding(10)
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
                         columns.ConstantColumn(180); // Increased from 120 to 180 for date/duration
                     });

                     var workExperiences = profile.WorkExperience.OrderByDescending(e => e.StartDate).ToList();
                     for (int i = 0; i < workExperiences.Count; i++)
                     {
                         var exp = workExperiences[i];
                         var isLast = (i == workExperiences.Count - 1);
                         
                         // Row 1: Title & Date
                         table.Cell().Text(StripHtml(exp.JobTitle ?? "")).Bold().FontSize(11).FontColor(TextDark);
                         table.Cell().AlignRight().Text(t =>
                         {
                             t.DefaultTextStyle(s => s.FontSize(8).FontColor(TextMedium));
                             t.Span($"{exp.StartDate:MM/yyyy} – {(exp.EndDate.HasValue ? exp.EndDate.Value.ToString("MM/yyyy") : "Present")}");
                             var duration = CalculateDuration(exp.StartDate, exp.EndDate);
                             if (!string.IsNullOrEmpty(duration))
                             {
                                 t.Span($" ({duration})").Italic();
                             }
                         });
                         
                         // Row 2: Company
                         var companyText = StripHtml(exp.CompanyName ?? "");
                         if (!string.IsNullOrEmpty(exp.Location)) companyText += $" - {StripHtml(exp.Location)}";
                         table.Cell().ColumnSpan(2).Text(companyText).FontSize(10).FontColor(PrimaryColor).SemiBold(); 

                         // Row 3: Description with Bullets
                         if (!string.IsNullOrWhiteSpace(exp.Description))
                         {
                             var desc = StripHtml(exp.Description, "▸ ");
                             table.Cell().ColumnSpan(2).PaddingTop(0.2f, Unit.Centimetre)
                                  .Text(desc).FontSize(9).FontColor(TextMedium).LineHeight(1.5f);
                         }
                         
                         // Grey Divider Line (only if not last)
                         if (!isLast)
                         {
                             table.Cell().ColumnSpan(2).PaddingTop(0.4f, Unit.Centimetre).PaddingBottom(0.4f, Unit.Centimetre)
                                  .LineHorizontal(1).LineColor(BorderColor);
                         }
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
                              cell.Background(BackgroundLight).BorderLeft(1.5f).BorderColor(AccentColor).CornerRadius(5).Padding(10).Column(c => 
                              {
                                  c.Item().Row(r => 
                                  {
                                      r.RelativeItem().Text(StripHtml(edu.Degree ?? "")).Bold().FontSize(11).FontColor(TextDark);
                                      r.ConstantItem(100).AlignRight().Text($"{edu.StartDate:yyyy} - {(edu.EndDate.HasValue ? edu.EndDate.Value.ToString("yyyy") : "Present")}").FontSize(8).FontColor(TextMedium);
                                  });
                                  
                                  c.Item().Text(StripHtml(edu.InstitutionName ?? "")).FontSize(10).FontColor(PrimaryColor).SemiBold().Bold();

                                  if (!string.IsNullOrEmpty(edu.Description))
                                  {
                                      // Handle **bold** markdown like HTML does
                                      c.Item().PaddingTop(0.2f, Unit.Centimetre).Text(t =>
                                      {
                                          t.DefaultTextStyle(s => s.FontSize(9).FontColor(TextMedium).LineHeight(1.5f));
                                          FormatMarkdownToText(t, edu.Description);
                                      });
                                  }
                              });
                          });
                      }
                 });
                 col.Item().PaddingBottom(0.5f, Unit.Centimetre);
            }

            // Projects
            if (profile.Projects != null && profile.Projects.Count !=0)
            {
                SectionTitle(col, "Personal Projects");
                
                 col.Item().Table(table => {
                     table.ColumnsDefinition(columns => {
                         columns.RelativeColumn();
                     });

                     foreach(var proj in profile.Projects.OrderByDescending(p => p.StartDate))
                     {
                         table.Cell().PaddingBottom(0.5f, Unit.Centimetre).Element(cell => 
                         {
                             cell.Background(BackgroundLight).BorderLeft(1.5f).BorderColor(PrimaryColor).CornerRadius(5).Padding(10).Column(c => 
                             {
                                 c.Item().Row(r => {
                                     r.RelativeItem().Text(StripHtml(proj.Name ?? "")).Bold().FontSize(11).FontColor(TextDark);
                                     
                                     string dateStr = "";
                                     if (proj.StartDate.HasValue)
                                          dateStr = $"{proj.StartDate:MMM yyyy} - {(proj.EndDate.HasValue ? proj.EndDate.Value.ToString("MMM yyyy") : "Present")}";
                                     r.ConstantItem(120).AlignRight().Text(dateStr).FontSize(8).FontColor(TextMedium);
                                 });

                                 if (!string.IsNullOrEmpty(proj.Link) || !string.IsNullOrEmpty(proj.Technologies))
                                 {
                                     c.Item().PaddingBottom(0.1f, Unit.Centimetre).Text(t => 
                                     {
                                         t.DefaultTextStyle(x => x.FontSize(9).FontColor(TextMedium));
                                         if (!string.IsNullOrEmpty(proj.Link))
                                         {
                                             t.Span("GitHub: ").Bold().FontColor(PrimaryDark);
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
                                      var desc = StripHtml(proj.Description, "✓ ");

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
                col.Item().Text(t => 
                {
                     t.DefaultTextStyle(x => x.FontSize(10).FontColor(TextMedium));
                     var langTexts = new List<string>();
                     foreach(var lang in profile.Languages)
                     {
                         langTexts.Add($"{StripHtml(lang.Name)} ({StripHtml(lang.Proficiency)})");
                     }
                     // Bold language names: render each separately
                     for (int i = 0; i < profile.Languages.Count; i++)
                     {
                         var lang = profile.Languages[i];
                         t.Span(StripHtml(lang.Name)).Bold();
                         t.Span($" ({StripHtml(lang.Proficiency)})");
                         if (i < profile.Languages.Count - 1)
                         {
                             t.Span(" | ");
                         }
                     }
                });
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }
            
             if (profile.Interests != null && profile.Interests.Count != 0)
            {
                SectionTitle(col, "Interests");
                
                // Tags Layout: Single row with bullet separators
                col.Item().Text(t => 
                {
                    t.DefaultTextStyle(x => x.FontSize(9).FontColor(TextMedium));
                    var interestNames = profile.Interests.Select(i => StripHtml(i.Name)).ToList();
                    t.Span(string.Join(" • ", interestNames));
                });
            }

            // Footer Reference (matching CSS: grey background, padding, top border)
            col.Item().BorderTop(1).BorderColor(BorderColor).Background(BackgroundLight)
               .PaddingVertical(1, Unit.Centimetre).AlignCenter()
               .Text("References available upon request").FontSize(9).FontColor(TextMedium).Italic();
        });
    }

    private static void SectionTitle(ColumnDescriptor column, string title)
    {
         column.Item().PaddingBottom(0.3f, Unit.Centimetre)
               .Row(row => 
               {
                   // CSS: display: inline-block; border-bottom: ... (Matches content width)
                   row.AutoItem().BorderBottom(1.5f).BorderColor(PrimaryColor) 
                      .Text(title.ToUpper()).FontSize(12).Bold().FontColor(PrimaryDark).LetterSpacing(0.06f);
               });
    }

    private static void FormatMarkdownToText(dynamic textDescriptor, string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        
        // Strip HTML first
        var cleaned = System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty).Trim();
        
        // Split by ** for bold formatting
        var parts = cleaned.Split(new[] { "**" }, StringSplitOptions.None);
        
        for (int i = 0; i < parts.Length; i++)
        {
            if (i % 2 == 0)
            {
                // Regular text
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    textDescriptor.Span(parts[i]);
                }
            }
            else
            {
                // Bold text (between **)
                if (!string.IsNullOrEmpty(parts[i]))
                {
                    textDescriptor.Span(parts[i]).Bold();
                }
            }
        }
    }

    private static string CalculateDuration(DateTime? start, DateTime? end)
    {
        if (!start.HasValue) return "";
        
        var endDate = end ?? DateTime.Now;
        var totalMonths = ((endDate.Year - start.Value.Year) * 12) + endDate.Month - start.Value.Month + 1;

        var years = totalMonths / 12;
        var months = totalMonths % 12;

        var parts = new List<string>();
        if (years > 0) parts.Add($"{years} year{(years > 1 ? "s" : "")}");
        if (months > 0) parts.Add($"{months} month{(months > 1 ? "s" : "")}");
        
        return string.Join(" ", parts);
    }

    private static string StripHtml(string input, string bullet = "\u2022 ")
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        
        // If already contains HTML li tags, process them
        if (input.Contains("<li>", StringComparison.OrdinalIgnoreCase))
        {
            var text = System.Text.RegularExpressions.Regex.Replace(input, "<li>", bullet, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, "</li>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, "<ul>|</ul>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = text.Replace("&#8226;", "\u2022 ");
            return System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty).Trim();
        }
        
        // Strip all HTML tags first
        var cleaned = System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty).Trim();
        
        // Only convert plain text to bullets if a custom bullet was specified (for descriptions)
        // If default bullet (\u2022), this is likely a title/name field - just return cleaned text
        if (bullet != "\u2022 " && cleaned.Contains('\n'))
        {
            // Plain text description: split by newlines and add bullets
            var lines = cleaned.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new System.Text.StringBuilder();
            
            foreach (var line in lines)
            {
                var cleanLine = line.Trim().TrimStart('-', '*').Trim();
                if (!string.IsNullOrEmpty(cleanLine))
                {
                    result.AppendLine($"{bullet}{cleanLine}");
                }
            }
            
            return result.ToString().Trim();
        }
        
        return cleaned;
    }
}
