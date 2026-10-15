namespace AiCV.Tests.Common;

public class JsonSanitizerTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public void Sanitize_NullOrWhitespace_ReturnsExpected(string? input, string expected)
    {
        var result = JsonSanitizer.Sanitize(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Sanitize_RawNewlineInString_EscapesCorrectly()
    {
        // Literal 0x0A inside a JSON string property
        const string rawJson = "{\"Description\":\"Line 1\nLine 2\"}";

        string sanitized = JsonSanitizer.Sanitize(rawJson);

        Assert.Equal("{\"Description\":\"Line 1\\nLine 2\"}", sanitized);

        // Verify System.Text.Json can now deserialize it without throwing
        using var doc = JsonDocument.Parse(sanitized);
        Assert.Equal("Line 1\nLine 2", doc.RootElement.GetProperty("Description").GetString());
    }

    [Fact]
    public void Sanitize_RawCarriageReturnAndTabInString_EscapesCorrectly()
    {
        const string rawJson = "{\"Text\":\"Col1\tCol2\r\nCol3\"}";

        string sanitized = JsonSanitizer.Sanitize(rawJson);

        Assert.Equal("{\"Text\":\"Col1\\tCol2\\r\\nCol3\"}", sanitized);

        using var doc = JsonDocument.Parse(sanitized);
        Assert.Equal("Col1\tCol2\r\nCol3", doc.RootElement.GetProperty("Text").GetString());
    }

    [Fact]
    public void Sanitize_AlreadyEscapedSequences_ArePreserved()
    {
        const string validJson = "{\"Description\":\"Already\\nescaped\\ttext and \\\"quotes\\\"\"}";

        string sanitized = JsonSanitizer.Sanitize(validJson);

        Assert.Equal(validJson, sanitized);

        using var doc = JsonDocument.Parse(sanitized);
        Assert.Equal("Already\nescaped\ttext and \"quotes\"", doc.RootElement.GetProperty("Description").GetString());
    }

    [Fact]
    public void Sanitize_StructuralNewlinesOutsideStrings_ArePreserved()
    {
        const string formattedJson = "{\n  \"Title\": \"Software Engineer\",\n  \"Score\": 95\n}";

        string sanitized = JsonSanitizer.Sanitize(formattedJson);

        Assert.Equal(formattedJson, sanitized);

        using var doc = JsonDocument.Parse(sanitized);
        Assert.Equal("Software Engineer", doc.RootElement.GetProperty("Title").GetString());
    }

    [Fact]
    public void Sanitize_CandidateProfileWithUnescapedWorkExperienceDescription_DeserializesSuccessfully()
    {
        // Replicates the exact error case from user:
        // '0x0A' is invalid within a JSON string. Path: $.WorkExperience[1].Description
        string rawProfileJson = "{\n" +
            "  \"FullName\": \"Test User\",\n" +
            "  \"WorkExperience\": [\n" +
            "    {\n" +
            "      \"Company\": \"Company A\",\n" +
            "      \"Description\": \"Normal description\"\n" +
            "    },\n" +
            "    {\n" +
            "      \"Company\": \"Company B\",\n" +
            "      \"Description\": \"Bullet 1" + (char)0x0A + "Bullet 2" + (char)0x0A + "Bullet 3\"\n" +
            "    }\n" +
            "  ]\n" +
            "}";

        // Confirm standard deserialization throws on raw JSON
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CandidateProfile>(rawProfileJson));

        // Sanitize and deserialize
        string sanitized = JsonSanitizer.Sanitize(rawProfileJson);
        var profile = JsonSerializer.Deserialize<CandidateProfile>(sanitized);

        Assert.NotNull(profile);
        Assert.Equal("Test User", profile.FullName);
        Assert.NotNull(profile.WorkExperience);
        Assert.Equal(2, profile.WorkExperience.Count);
        Assert.Contains("Bullet 1\nBullet 2\nBullet 3", profile.WorkExperience[1].Description);
    }
}

