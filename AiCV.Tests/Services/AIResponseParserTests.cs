namespace AiCV.Tests.Services;

public class AIResponseParserTests
{
    [Theory]
    [InlineData("Here is a professional cover letter:\n\nMay 28, 2026", "May 28, 2026")]
    [InlineData("Certainly, here is a tailored cover letter for the role:\r\nCompany\r\nDate", "Company\r\nDate")]
    [InlineData("Below is your cover letter:\nSubject: Application", "Subject: Application")]
    public void ParseCoverLetter_RemovesModelPreamble(string input, string expected)
    {
        var result = AIResponseParser.ParseCoverLetter(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseCoverLetter_KeepsActualLetterContent()
    {
        const string input = "May 28, 2026\n\nSubject: Application for Software Engineer";

        var result = AIResponseParser.ParseCoverLetter(input);

        Assert.Equal(input, result);
    }
}
