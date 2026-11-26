using System.Text.RegularExpressions;

namespace DoggoDex.Api.Common;

public enum SearchKind
{
    Text,
    Id,
    Slug
}

public sealed record ParsedQuery(SearchKind Kind, string Value);

/// <summary>
/// Decides whether a user-typed search term is free text, a profile id or a profile slug.
/// Accepts absolute profile links, relative "/p/slug" links and bare slugs.
/// </summary>
public static class ProfileLinkParser
{
    private static readonly Regex SlugPattern = new(
        "^[a-z0-9]+(-[a-z0-9]+)+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static ParsedQuery Parse(string? input)
    {
        var trimmed = input?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return new ParsedQuery(SearchKind.Text, string.Empty);
        }

        var candidate = ExtractCandidate(trimmed) ?? trimmed;

        if (candidate.Length == 0)
        {
            return new ParsedQuery(SearchKind.Text, string.Empty);
        }

        if (Guid.TryParse(candidate, out var id))
        {
            return new ParsedQuery(SearchKind.Id, id.ToString());
        }

        if (candidate.All(char.IsAsciiDigit))
        {
            return new ParsedQuery(SearchKind.Id, candidate);
        }

        var lowered = candidate.ToLowerInvariant();
        if (SlugPattern.IsMatch(lowered))
        {
            return new ParsedQuery(SearchKind.Slug, lowered);
        }

        return new ParsedQuery(SearchKind.Text, candidate);
    }

    /// <summary>Returns the last path segment for links, or null when the input is not a link.</summary>
    private static string? ExtractCandidate(string trimmed)
    {
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return LastPathSegment(uri.AbsolutePath);
        }

        if (trimmed.StartsWith('/'))
        {
            var path = trimmed;
            var cutoff = path.IndexOfAny(['?', '#']);
            if (cutoff >= 0)
            {
                path = path[..cutoff];
            }

            return LastPathSegment(path);
        }

        return null;
    }

    private static string LastPathSegment(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return segments.Length == 0 ? string.Empty : Uri.UnescapeDataString(segments[^1]);
    }
}
