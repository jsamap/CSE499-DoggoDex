using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace DoggoDex.Api.Common;

/// <summary>
/// Builds URL-friendly profile slugs: lowercase, accent-free, a-z/0-9 words joined by
/// single hyphens, with a short random suffix appended to keep them unique.
/// </summary>
public static class SlugGenerator
{
    public const int MaxBaseLength = 40;
    public const int SuffixLength = 5;
    public const string FallbackBase = "profile";
    public const string SuffixAlphabet = "abcdefghjkmnpqrstuvwxyz23456789";

    /// <summary>"Max the Dog" becomes "max-the-dog"; returns "" when nothing usable is left.</summary>
    public static string Slugify(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var decomposed = name.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        // Starts true so a leading run of separators does not produce a leading hyphen.
        var lastWasHyphen = true;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(character);
                lastWasHyphen = false;
            }
            else if (character is >= 'A' and <= 'Z')
            {
                builder.Append(char.ToLowerInvariant(character));
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        var slug = builder.ToString().TrimEnd('-');

        if (slug.Length > MaxBaseLength)
        {
            slug = slug[..MaxBaseLength].TrimEnd('-');
        }

        return slug;
    }

    /// <summary>Appends a random 5-character suffix, e.g. "max-the-dog-k7p2q".</summary>
    public static string Generate(string? name) => Generate(name, RandomSuffix());

    /// <summary>Overload used by tests and by collision retries.</summary>
    public static string Generate(string? name, string? suffix)
    {
        var baseSlug = Slugify(name);
        if (baseSlug.Length == 0)
        {
            baseSlug = FallbackBase;
        }

        var normalizedSuffix = NormalizeSuffix(suffix);
        return normalizedSuffix.Length == 0 ? baseSlug : $"{baseSlug}-{normalizedSuffix}";
    }

    /// <summary>Five characters from the collision-free alphabet, using a cryptographic RNG.</summary>
    public static string RandomSuffix()
    {
        var characters = new char[SuffixLength];
        for (var i = 0; i < SuffixLength; i++)
        {
            characters[i] = SuffixAlphabet[RandomNumberGenerator.GetInt32(SuffixAlphabet.Length)];
        }

        return new string(characters);
    }

    private static string NormalizeSuffix(string? suffix)
    {
        if (string.IsNullOrWhiteSpace(suffix))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(suffix.Length);
        foreach (var character in suffix.ToLowerInvariant())
        {
            if (SuffixAlphabet.Contains(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
