using DoggoDex.Api.Common;

namespace DoggoDex.Tests.Common;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Max the Dog", "max-the-dog")]
    [InlineData("  Max the Dog  ", "max-the-dog")]
    [InlineData("Max   the---Dog", "max-the-dog")]
    [InlineData("Max_the_Dog", "max-the-dog")]
    [InlineData("Max!", "max")]
    [InlineData("Café Ñandú!", "cafe-nandu")]
    [InlineData("Sky", "sky")]
    [InlineData("Bella 2", "bella-2")]
    public void Slugify_ProducesLowercaseHyphenatedSlug(string name, string expected)
    {
        Assert.Equal(expected, SlugGenerator.Slugify(name));
    }

    [Fact]
    public void Slugify_TruncatesBaseAtFortyCharactersWithoutTrailingHyphen()
    {
        var slug = SlugGenerator.Slugify(new string('a', 30) + " " + new string('b', 30));

        Assert.Equal(40, slug.Length);
        Assert.EndsWith("b", slug);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    [InlineData("---")]
    public void Slugify_ReturnsEmptyWhenNothingUsableIsLeft(string? name)
    {
        Assert.Equal(string.Empty, SlugGenerator.Slugify(name));
    }

    [Fact]
    public void Generate_AppendsSuffixFromAlphabet()
    {
        var slug = SlugGenerator.Generate("Max the Dog");

        Assert.StartsWith("max-the-dog-", slug);
        var suffix = slug["max-the-dog-".Length..];
        Assert.Equal(SlugGenerator.SuffixLength, suffix.Length);
        Assert.All(suffix, character => Assert.Contains(character, SlugGenerator.SuffixAlphabet));
    }

    [Fact]
    public void Generate_ProducesDifferentSlugsForTheSameName()
    {
        var slugs = Enumerable.Range(0, 25).Select(_ => SlugGenerator.Generate("Max")).ToHashSet();

        Assert.True(slugs.Count > 20, "Expected the random suffix to spread 25 generated slugs.");
    }

    [Theory]
    [InlineData("Max the Dog", "k7p2q", "max-the-dog-k7p2q")]
    [InlineData("Max the Dog", "K7P2Q", "max-the-dog-k7p2q")]
    [InlineData("", "abcd2", "profile-abcd2")]
    [InlineData("!!!", "abcd2", "profile-abcd2")]
    [InlineData("Max", "ab1!", "max-ab")]
    [InlineData("Max", "", "max")]
    [InlineData("Max", null, "max")]
    public void Generate_UsesFallbackBaseAndNormalizesSuffix(string? name, string? suffix, string expected)
    {
        Assert.Equal(expected, SlugGenerator.Generate(name, suffix));
    }
}
