using System.Text.RegularExpressions;
using DoggoDex.Shared.Profiles;
using DoggoDex.Shared.Search;

namespace DoggoDex.Web.Api;

/// <summary>
/// In-memory stand-in for GET /api/search and GET /api/profiles/{slug}. It mirrors the API's
/// behaviour — query classification (Browse / Text / Slug / Id), name-then-id ordering, paging
/// clamps and the "rating is null when there are no reviews" rule — so pages written against it
/// keep working when <see cref="HttpProfileApi"/> is switched on.
/// </summary>
public sealed partial class MockProfileApi : IProfileApi
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    private readonly IReadOnlyList<MockProfile> profiles;

    public MockProfileApi()
        : this(MockProfileData.Profiles)
    {
    }

    public MockProfileApi(IReadOnlyList<MockProfile> profiles) => this.profiles = profiles;

    public Task<SearchResponse> SearchAsync(
        string? q,
        ProfileTypeFilter type = ProfileTypeFilter.All,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);

        var (kind, term) = Classify(q);
        var source = FilterByType(type);

        string matchType;
        IEnumerable<MockProfile> matches;

        if (kind == MockSearchKind.Id && Guid.TryParse(term, out var id))
        {
            matchType = "Id";
            matches = Order(source.Where(profile => profile.Id == id));
        }
        else if (kind == MockSearchKind.Slug)
        {
            var exact = source
                .Where(profile => Is(profile.Slug, term) || Is(profile.ShareableId, term))
                .ToList();

            if (exact.Count > 0)
            {
                // A real slug hit outranks a legacy shareable-id hit, exactly like the API.
                matchType = "Slug";
                matches = Order(exact, profile => Is(profile.Slug, term));
            }
            else
            {
                // Slug-shaped input with nothing behind it: read the hyphens as spaces.
                matchType = "Text";
                matches = Order(NameMatches(source, term.Replace('-', ' ')));
            }
        }
        else if (term.Length == 0)
        {
            matchType = "Browse";
            matches = Order(source);
        }
        else
        {
            // Free text, and digits that are not a GUID: fall through to a name search.
            matchType = "Text";
            matches = Order(NameMatches(source, term));
        }

        var ordered = matches.ToList();

        return Task.FromResult(new SearchResponse
        {
            Query = term,
            MatchType = matchType,
            Items = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToResultItem)
                .ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = ordered.Count
        });
    }

    public Task<ProfileLookupResponse?> GetProfileAsync(string slug, CancellationToken cancellationToken = default)
    {
        var key = slug?.Trim() ?? string.Empty;
        if (key.Length == 0)
        {
            return Task.FromResult<ProfileLookupResponse?>(null);
        }

        // Owners win over dogs, and a slug match wins over a legacy shareable id.
        var bySlug = profiles.Where(profile => Is(profile.Slug, key));
        var match = PreferOwners(bySlug).FirstOrDefault()
            ?? PreferOwners(profiles.Where(profile => Is(profile.ShareableId, key))).FirstOrDefault();

        if (match is null)
        {
            return Task.FromResult<ProfileLookupResponse?>(null);
        }

        return Task.FromResult<ProfileLookupResponse?>(new ProfileLookupResponse
        {
            Type = match.Type,
            Id = match.Id,
            Slug = match.Slug,
            Name = match.Name,
            PhotoUrl = match.PhotoUrl,
            OwnerName = match.OwnerName,
            OwnerSlug = match.OwnerSlug,
            Dogs = match.Type == MockProfileData.OwnerType
                ? profiles
                    .Where(dog => dog.Type == MockProfileData.DogType && Is(dog.OwnerSlug, match.Slug))
                    .OrderBy(dog => dog.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(dog => dog.Id)
                    .Select(dog => new ProfileDogSummary { Name = dog.Name, Slug = dog.Slug })
                    .ToList()
                : [],
            AverageRating = Rating(match),
            ReviewCount = match.ReviewCount
        });
    }

    private enum MockSearchKind
    {
        Text,
        Id,
        Slug
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)+$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();

    private IEnumerable<MockProfile> FilterByType(ProfileTypeFilter type) => type switch
    {
        ProfileTypeFilter.Owners => profiles.Where(profile => profile.Type == MockProfileData.OwnerType),
        ProfileTypeFilter.Dogs => profiles.Where(profile => profile.Type == MockProfileData.DogType),
        _ => profiles
    };

    /// <summary>Relevance first (a slug hit outranks a shareable-id hit), then name, then id.</summary>
    private static IEnumerable<MockProfile> Order(
        IEnumerable<MockProfile> items,
        Func<MockProfile, bool>? relevance = null)
    {
        var decorated = relevance is null
            ? items.Select(profile => (Profile: profile, Hit: false))
            : items.Select(profile => (Profile: profile, Hit: relevance(profile)));

        return decorated
            .OrderByDescending(row => row.Hit)
            .ThenBy(row => row.Profile.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Profile.Id)
            .Select(row => row.Profile);
    }

    /// <summary>Every whitespace-separated token must appear in the name (AND), like the API.</summary>
    private static IEnumerable<MockProfile> NameMatches(IEnumerable<MockProfile> items, string term)
    {
        var tokens = term.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return tokens.Length == 0
            ? []
            : items.Where(profile => tokens.All(token => profile.Name.Contains(token, StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<MockProfile> PreferOwners(IEnumerable<MockProfile> items) =>
        items.OrderBy(profile => profile.Type == MockProfileData.OwnerType ? 0 : 1);

    private static bool Is(string? value, string key) =>
        string.Equals(value, key, StringComparison.OrdinalIgnoreCase);

    /// <summary>The API nulls the rating while a profile has no reviews; keep that rule here.</summary>
    private static double? Rating(MockProfile profile) =>
        profile.ReviewCount == 0 ? null : profile.AverageRating;

    private static SearchResultItem ToResultItem(MockProfile profile) => new()
    {
        Type = profile.Type,
        Id = profile.Id,
        Slug = profile.Slug,
        Name = profile.Name,
        Breed = profile.Breed,
        PhotoUrl = profile.PhotoUrl,
        OwnerName = profile.OwnerName,
        OwnerSlug = profile.OwnerSlug,
        AverageRating = Rating(profile),
        ReviewCount = profile.ReviewCount,
        ShareableId = profile.ShareableId
    };

    /// <summary>
    /// Mirrors <c>DoggoDex.Api.Common.ProfileLinkParser</c>, which lives in the API project and so
    /// cannot be referenced from here. Moves to DoggoDex.Shared once the API PRs land, and this copy
    /// goes away.
    /// </summary>
    private static (MockSearchKind Kind, string Term) Classify(string? input)
    {
        var trimmed = input?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return (MockSearchKind.Text, string.Empty);
        }

        var candidate = ExtractCandidate(trimmed) ?? trimmed;
        if (candidate.Length == 0)
        {
            return (MockSearchKind.Text, string.Empty);
        }

        if (Guid.TryParse(candidate, out var id))
        {
            return (MockSearchKind.Id, id.ToString());
        }

        if (candidate.All(char.IsAsciiDigit))
        {
            return (MockSearchKind.Id, candidate);
        }

        var lowered = candidate.ToLowerInvariant();
        return SlugPattern().IsMatch(lowered)
            ? (MockSearchKind.Slug, lowered)
            : (MockSearchKind.Text, candidate);
    }

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

