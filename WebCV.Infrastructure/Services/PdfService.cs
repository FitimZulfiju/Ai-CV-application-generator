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

    private static readonly Dictionary<string, string> NamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        { "black", "#000000" }, { "white", "#FFFFFF" }, { "red", "#FF0000" }, { "lime", "#00FF00" }, { "blue", "#0000FF" },
        { "yellow", "#FFFF00" }, { "cyan", "#00FFFF" }, { "magenta", "#FF00FF" }, { "silver", "#C0C0C0" }, { "gray", "#808080" },
        { "grey", "#808080" }, { "maroon", "#800000" }, { "olive", "#808000" }, { "green", "#008000" }, { "purple", "#800080" },
        { "teal", "#008080" }, { "navy", "#000080" }, { "orange", "#FFA500" }
    };

    private static string? GetHexColor(string? colorName)
    {
        if (string.IsNullOrWhiteSpace(colorName)) return null;
        colorName = colorName.Trim();
        if (!colorName.StartsWith("#"))
        {
            if (NamedColors.TryGetValue(colorName, out var hex)) return hex;
            return null;
        }

        return colorName;
    }

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
                        // Summary Text with HTML formatting support
                        c.Item().Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontColor(TextMedium).FontSize(10).LineHeight(1.5f));
                            FormatHtmlToText(t, PreprocessHtml(profile.ProfessionalSummary));
                        });
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
                             table.Cell().ColumnSpan(2).PaddingTop(0.2f, Unit.Centimetre).Text(t => { t.DefaultTextStyle(dt => dt.FontSize(9).FontColor(TextMedium).LineHeight(1.5f)); FormatHtmlToText(t, PreprocessHtml(exp.Description, "\u25B8 ")); });
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
                                      // Handle HTML tags like <strong style=color:blue;>
                                      c.Item().PaddingTop(0.2f, Unit.Centimetre).Text(t =>
                                      {
                                          t.DefaultTextStyle(s => s.FontSize(9).FontColor(TextMedium).LineHeight(1.5f));
                                          FormatHtmlToText(t, PreprocessHtml(edu.Description));
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
                                          dateStr = $"{proj.StartDate.Value:yyyy} - {(proj.EndDate.HasValue ? proj.EndDate.Value.ToString("yyyy") : "Present")}";
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
                                      c.Item().PaddingTop(0.1f, Unit.Centimetre).Text(t => { t.DefaultTextStyle(dt => dt.FontSize(9).FontColor(TextMedium).LineHeight(1.5f)); FormatHtmlToText(t, PreprocessHtml(proj.Description, "\u2713 ")); });
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
                // Use Inlined to flow items like tags
                col.Item().PaddingBottom(0.5f, Unit.Centimetre).Inlined(w => 
                {
                    w.Spacing(10);
                    foreach(var lang in profile.Languages)
                    {
                        w.Item().Text(t => 
                        {
                            t.DefaultTextStyle(dt => dt.FontSize(10).FontColor(TextMedium));
                            t.Span(StripHtml(lang.Name)).Bold();
                            t.Span($" ({StripHtml(lang.Proficiency)})");
                        });
                    }
                });
                col.Item().PaddingBottom(1, Unit.Centimetre);
            }
            
             if (profile.Interests != null && profile.Interests.Count != 0)
            {
                SectionTitle(col, "Interests");
                
                // Tags Layout: Chips with background
                col.Item().Inlined(w => 
                {
                    w.Spacing(5);
                    foreach(var interest in profile.Interests)
                    {
                        w.Item().Background(BackgroundLight).PaddingHorizontal(8).PaddingVertical(4).CornerRadius(4)
                         .Text(StripHtml(interest.Name)).FontSize(9).FontColor(TextMedium);
                    }
                });
            }

            // Footer Reference
            col.Item().PaddingTop(1, Unit.Centimetre).AlignCenter()
               .Text("References available upon request").FontSize(10).FontColor(TextMedium);
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

    private static void FormatHtmlToText(TextDescriptor textDescriptor, string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        
        // Regex to match HTML tags with optional inline styles (supporting unquoted, single-quoted, and double-quoted)
        var tagPattern = @"<(strong|b|em|i|u|span)(?:\s+style\s*=\s*(?:""([^""]*)""|'([^']*)'|([^""'\s>]+)))?\s*>(.+?)</\1>";
        var regex = new Regex(tagPattern, RegexOptions.IgnoreCase);
        
        int lastIndex = 0;
        foreach (Match match in regex.Matches(input))
        {
            // Add text before this match (strip any remaining HTML tags)
            if (match.Index > lastIndex)
            {
                var beforeText = input[lastIndex..match.Index];
                var cleanBefore = Regex.Replace(beforeText, "<.*?>", string.Empty);
                if (!string.IsNullOrEmpty(cleanBefore))
                {
                    textDescriptor.Span(cleanBefore);
                }
            }
            
            var tagName = match.Groups[1].Value.ToLower();
            // Style attr can be in group 2 (double), 3 (single), or 4 (unquoted)
            var styleAttr = match.Groups[2].Value + match.Groups[3].Value + match.Groups[4].Value;
            var content = match.Groups[5].Value;
            
            // Strip nested HTML from content (simple handling)
            var cleanContent = Regex.Replace(content, "<.*?>", string.Empty);
            
            // Initial state based on tag
            bool isBold = tagName == "strong" || tagName == "b";
            bool isItalic = tagName == "em" || tagName == "i";
            string? color = null;
            
            if (!string.IsNullOrEmpty(styleAttr))
            {
                // Parse color
                var colorMatch = Regex.Match(styleAttr, @"color\s*:\s*([^;]+)", RegexOptions.IgnoreCase);
                if (colorMatch.Success)
                {
                    var rawColor = colorMatch.Groups[1].Value.Trim();
                    color = GetHexColor(rawColor);
                }
                
                // Parse font-weight
                var weightMatch = Regex.Match(styleAttr, @"font-weight\s*:\s*([^;]+)", RegexOptions.IgnoreCase);
                if (weightMatch.Success)
                {
                    var weight = weightMatch.Groups[1].Value.Trim().ToLower();
                    if (weight == "bold" || weight == "700" || weight == "800" || weight == "900") isBold = true;
                    if (weight == "normal" || weight == "400") isBold = false;
                }
                
                // Parse font-style
                var styleMatch = Regex.Match(styleAttr, @"font-style\s*:\s*([^;]+)", RegexOptions.IgnoreCase);
                if (styleMatch.Success)
                {
                    var style = styleMatch.Groups[1].Value.Trim().ToLower();
                    if (style == "italic") isItalic = true;
                    if (style == "normal") isItalic = false;
                }
            }
            
            // Apply styling
            var span = textDescriptor.Span(cleanContent);
            if (isBold) span.Bold();
            if (isItalic) span.Italic();
            if (!string.IsNullOrEmpty(color)) span.FontColor(color);
            
            lastIndex = match.Index + match.Length;
        }
        
        // Add remaining text
        if (lastIndex < input.Length)
        {
            var remainingText = input[lastIndex..];
            var cleanRemaining = Regex.Replace(remainingText, "<.*?>", string.Empty);
            if (!string.IsNullOrEmpty(cleanRemaining))
            {
                textDescriptor.Span(cleanRemaining);
            }
        }
    }

    private static string PreprocessHtml(string? input, string bullet = "")
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        string pText = input ?? "";

        // Handle Lists (<ul><li>...</li></ul>)
        if (pText.Contains("<li>", StringComparison.OrdinalIgnoreCase))
        {
            // Replace <li> with bullet
             pText = Regex.Replace(pText, "<li>", !string.IsNullOrEmpty(bullet) ? bullet : "• ", RegexOptions.IgnoreCase);
             // Replace </li> with newline
             pText = Regex.Replace(pText, "</li>", "\n", RegexOptions.IgnoreCase);
             pText = Regex.Replace(pText, "<ul>|</ul>|<ol>|</ol>", "", RegexOptions.IgnoreCase);
             pText = pText.Replace("&#8226;", "• ");
        }
        else if (!string.IsNullOrEmpty(bullet) && pText.Contains('\n') && !pText.Contains("<p>", StringComparison.OrdinalIgnoreCase))
        {
            // Plain text with newlines - convert to bullets if requested
             var lines = pText.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
             var sb = new StringBuilder();
             foreach (var line in lines)
             {
                 var cleanLine = line.Trim().TrimStart('-', '*').Trim();
                 if (!string.IsNullOrEmpty(cleanLine))
                     sb.AppendLine($"{bullet}{cleanLine}");
             }
             return sb.ToString().Trim();
        }

        // Convert <br> to newline
        pText = Regex.Replace(pText, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
        // Convert </p> to newline
        pText = Regex.Replace(pText, "</p>", "\n", RegexOptions.IgnoreCase);
        pText = Regex.Replace(pText, "<p.*?>", "", RegexOptions.IgnoreCase);

        // Decode HTML entities
        pText = System.Net.WebUtility.HtmlDecode(pText);

        return pText.Trim();
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

    // Keep strict StripHtml for titles/names where we want clean text only
    private static string StripHtml(string? input)
    {
         if (string.IsNullOrWhiteSpace(input)) return string.Empty;
         return Regex.Replace(input, "<.*?>", string.Empty).Trim();
    }
}

