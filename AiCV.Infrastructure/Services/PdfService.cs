namespace AiCV.Infrastructure.Services;

public class PdfService(IWebHostEnvironment env, IStringLocalizer<AicvResources> localizer)
    : IPdfService
{
    private readonly IWebHostEnvironment _env = env;
    private readonly IStringLocalizer<AicvResources> _localizer = localizer;

    private IPdfTemplateBuilder GetTemplateBuilder(CvTemplate template)
    {
        return template switch
        {
            CvTemplate.Modern => new ModernPdfTemplate(_env, _localizer),
            CvTemplate.Minimalist => new MinimalistPdfTemplate(_env, _localizer),
            _ => new ProfessionalPdfTemplate(_env, _localizer),
        };
    }

    private float FindOptimalFontSize(IPdfTemplateBuilder builder, float[] fontSizes, Action<QuestPDF.Fluent.PageDescriptor, float> composePage)
    {
        float optimalSize = fontSizes.LastOrDefault() == 0 ? 8f : fontSizes.Last();
        foreach (var size in fontSizes.Distinct())
        {
            var testDoc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.75f, Unit.Centimetre);
                    composePage(page, size);
                });
            });

            if (builder.GetPageCount(testDoc.GeneratePdf()) <= 1)
            {
                return size;
            }
        }
        return optimalSize;
    }

    public Task<byte[]> GenerateCvAsync(CandidateProfile profile, CvTemplate template)
    {
        var builder = GetTemplateBuilder(template);

        float[] fontSizes = [14f, 13.75f, 13.5f, 13.25f, 13f, 12.75f, 12.5f, 12.25f, 12f, 11.75f, 11.5f, 11.25f, 11f, 10.75f, 10.5f, 10.25f, 10f, 9.5f, 9f, 8.5f, 8f];
        float page1Size = FindOptimalFontSize(builder, fontSizes, (page, size) => 
        {
            page.Header().ShowOnce().Element(c => builder.ComposeHeader(c, profile));
            page.Content().Element(c => builder.ComposePageOne(c, profile, size));
        });

        float[] page2FontSizes = [16f, 15.75f, 15.5f, 15.25f, 15f, 14.75f, 14.5f, 14.25f, 14f, 13.75f, 13.5f, 13.25f, 13f, 12.75f, 12.5f, 12.25f, 12f, 11.75f, 11.5f, 11.25f, 11f, 10.75f, 10.5f, 10.25f, 10f, 9.5f, 9f, 8.5f, 8f];
        float page2Size = FindOptimalFontSize(builder, page2FontSizes, (page, size) => 
        {
            page.Content().Element(c => builder.ComposePageTwo(c, profile, size));
        });

        float[] page3FontSizes = [page2Size, 10f, 9.5f, 9f, 8.5f, 8f, 7.5f, 7f];
        float page3Size = FindOptimalFontSize(builder, page3FontSizes, (page, size) => 
        {
            page.Content().Element(c => builder.ComposePageThree(c, profile, size));
        });

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0.75f, Unit.Centimetre);
                page.Header().Element(c => builder.ComposeHeader(c, profile));
                page.Content().Element(c => builder.ComposePageOne(c, profile, page1Size));
            });
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0.75f, Unit.Centimetre);
                page.Content().Element(c => builder.ComposePageTwo(c, profile, page2Size));
            });
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0.75f, Unit.Centimetre);
                page.Content().Element(c => builder.ComposePageThree(c, profile, page3Size));
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    public Task<byte[]> GenerateCoverLetterAsync(
        string letterContent,
        CandidateProfile profile,
        string jobTitle,
        string companyName,
        CvTemplate template
    )
    {
        var builder = GetTemplateBuilder(template);
        var headerProfile = CreateCoverLetterHeaderProfile(profile);
        float[] fontSizes = [12f, 11.5f, 11f, 10.5f, 10f, 9.5f, 9f, 8.5f, 8f];
        
        float finalSize = FindOptimalFontSize(builder, fontSizes, (page, size) => 
        {
            page.Header().ShowOnce().Element(c => builder.ComposeHeader(c, headerProfile));
            page.Content().Element(c => builder.ComposeCoverLetter(c, letterContent, profile, size));
        });

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0.75f, Unit.Centimetre);
                page.Header().ShowOnce().Element(c => builder.ComposeHeader(c, headerProfile));
                page.Content().Element(c => builder.ComposeCoverLetter(c, letterContent, profile, finalSize));
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    private static CandidateProfile CreateCoverLetterHeaderProfile(CandidateProfile profile)
    {
        return new CandidateProfile
        {
            FullName = profile.FullName,
            Title = profile.Title,
            Email = profile.Email,
            PhoneNumber = profile.PhoneNumber,
            Location = profile.Location,
            LinkedInUrl = profile.LinkedInUrl,
            ShowProfilePicture = false,
            ProfilePictureUrl = string.Empty,
            PortfolioUrl = string.Empty,
            Tagline = string.Empty,
        };
    }
}
