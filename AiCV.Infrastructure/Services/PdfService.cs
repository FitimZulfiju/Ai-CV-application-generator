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

    public Task<byte[]> GenerateCvAsync(CandidateProfile profile, CvTemplate template)
    {
        var builder = GetTemplateBuilder(template);

        float[] fontSizes =
        [
            14f,
            13.75f,
            13.5f,
            13.25f,
            13f,
            12.75f,
            12.5f,
            12.25f,
            12f,
            11.75f,
            11.5f,
            11.25f,
            11f,
            10.75f,
            10.5f,
            10.25f,
            10f,
            9.5f,
            9f,
            8.5f,
            8f,
        ];

        float page1Size = 8f;
        foreach (var size in fontSizes)
        {
            var p1Doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.75f, Unit.Centimetre);
                    page.Header().ShowOnce().Element(c => builder.ComposeHeader(c, profile));
                    page.Content().Element(c => builder.ComposePageOne(c, profile, size));
                });
            });
            if (builder.GetPageCount(p1Doc.GeneratePdf()) <= 1)
            {
                page1Size = size;
                break;
            }
        }

        float[] page2FontSizes =
        [
            16f,
            15.75f,
            15.5f,
            15.25f,
            15f,
            14.75f,
            14.5f,
            14.25f,
            14f,
            13.75f,
            13.5f,
            13.25f,
            13f,
            12.75f,
            12.5f,
            12.25f,
            12f,
            11.75f,
            11.5f,
            11.25f,
            11f,
            10.75f,
            10.5f,
            10.25f,
            10f,
            9.5f,
            9f,
            8.5f,
            8f,
        ];
        float page2Size = 8f;
        foreach (var size in page2FontSizes)
        {
            var p2Doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.75f, Unit.Centimetre);
                    page.Content().Element(c => builder.ComposePageTwo(c, profile, size));
                });
            });
            if (builder.GetPageCount(p2Doc.GeneratePdf()) <= 1)
            {
                page2Size = size;
                break;
            }
        }

        float[] page3FontSizes =
        [
            page2Size,
            10f,
            9.5f,
            9f,
            8.5f,
            8f,
            7.5f,
            7f,
        ];
        float page3Size = page3FontSizes[0];
        foreach (var size in page3FontSizes.Distinct())
        {
            var p3Doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.75f, Unit.Centimetre);
                    page.Content().Element(c => builder.ComposePageThree(c, profile, size));
                });
            });
            if (builder.GetPageCount(p3Doc.GeneratePdf()) <= 1)
            {
                page3Size = size;
                break;
            }
        }

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
        float[] fontSizes = [12f, 11.5f, 11f, 10.5f, 10f, 9.5f, 9f, 8.5f, 8f];
        byte[] pdfBytes = [];

        foreach (var size in fontSizes)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(0.75f, Unit.Centimetre);
                    page.Header().ShowOnce().Element(c => builder.ComposeHeader(c, profile));
                    page.Content()
                        .Element(c => builder.ComposeCoverLetter(c, letterContent, profile, size));
                });
            });

            pdfBytes = document.GeneratePdf();
            if (builder.GetPageCount(pdfBytes) <= 1)
                return Task.FromResult(pdfBytes);
        }

        return Task.FromResult(pdfBytes);
    }
}
