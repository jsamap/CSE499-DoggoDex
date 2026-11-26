using DoggoDex.Api.Common;

namespace DoggoDex.Tests.Common;

public class ProfileLinkParserTests
{
    [Theory]
    [InlineData("Max the Dog")]
    [InlineData("  Max the Dog  ")]
    [InlineData("Max")]
    [InlineData("Café")]
    public void Parse_TreatsPlainWordsAsText(string input)
    {
        var parsed = ProfileLinkParser.Parse(input);

        Assert.Equal(SearchKind.Text, parsed.Kind);
        Assert.Equal(input.Trim(), parsed.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_TreatsEmptyInputAsEmptyText(string? input)
    {
        var parsed = ProfileLinkParser.Parse(input);

        Assert.Equal(SearchKind.Text, parsed.Kind);
        Assert.Equal(string.Empty, parsed.Value);
    }

    [Theory]
    [InlineData("max-the-dog-k7p2q")]
    [InlineData("MAX-THE-DOG-K7P2Q")]
    [InlineData("jared-samayoa")]
    public void Parse_RecognizesBareSlugsCaseInsensitively(string input)
    {
        var parsed = ProfileLinkParser.Parse(input);

        Assert.Equal(SearchKind.Slug, parsed.Kind);
        Assert.Equal(input.ToLowerInvariant(), parsed.Value);
    }

    [Theory]
    [InlineData("/p/max-the-dog-k7p2q", "max-the-dog-k7p2q")]
    [InlineData("/p/max-the-dog-k7p2q/", "max-the-dog-k7p2q")]
    [InlineData("/p/max-the-dog-k7p2q?ref=card", "max-the-dog-k7p2q")]
    [InlineData("/p/max-the-dog-k7p2q#top", "max-the-dog-k7p2q")]
    [InlineData("https://doggodex.app/p/max-the-dog-k7p2q", "max-the-dog-k7p2q")]
    [InlineData("https://doggodex.app/p/max-the-dog-k7p2q/", "max-the-dog-k7p2q")]
    [InlineData("http://localhost:5173/profiles/jared-samayoa?tab=dogs", "jared-samayoa")]
    public void Parse_ExtractsLastSegmentFromLinks(string input, string expected)
    {
        var parsed = ProfileLinkParser.Parse(input);

        Assert.Equal(SearchKind.Slug, parsed.Kind);
        Assert.Equal(expected, parsed.Value);
    }

    [Fact]
    public void Parse_LowercasesLinkSegments()
    {
        var parsed = ProfileLinkParser.Parse("https://doggodex.app/p/Max-The-Dog");

        Assert.Equal(SearchKind.Slug, parsed.Kind);
        Assert.Equal("max-the-dog", parsed.Value);
    }

    [Fact]
    public void Parse_RecognizesGuidAsId()
    {
        var id = Guid.NewGuid();

        var parsed = ProfileLinkParser.Parse($"  {id.ToString().ToUpperInvariant()}  ");

        Assert.Equal(SearchKind.Id, parsed.Kind);
        Assert.Equal(id.ToString(), parsed.Value);
    }

    [Fact]
    public void Parse_RecognizesCompactGuidAsId()
    {
        var id = Guid.NewGuid();

        var parsed = ProfileLinkParser.Parse(id.ToString("N"));

        Assert.Equal(SearchKind.Id, parsed.Kind);
        Assert.Equal(id.ToString(), parsed.Value);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("/p/12345")]
    public void Parse_RecognizesAllDigitInputAsId(string input)
    {
        var parsed = ProfileLinkParser.Parse(input);

        Assert.Equal(SearchKind.Id, parsed.Kind);
        Assert.Equal("12345", parsed.Value);
    }

    [Theory]
    [InlineData("https://doggodex.app")]
    [InlineData("https://doggodex.app/p/Caf%C3%A9")]
    public void Parse_FallsBackToTextWhenSegmentIsNotASlug(string input)
    {
        var parsed = ProfileLinkParser.Parse(input);

        Assert.Equal(SearchKind.Text, parsed.Kind);
    }
}
