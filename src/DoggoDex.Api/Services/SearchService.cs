using System.Linq.Expressions;
using DoggoDex.Api.Common;
using DoggoDex.Api.Infrastructure;
using DoggoDex.Shared.Search;
using Microsoft.EntityFrameworkCore;

namespace DoggoDex.Api.Services;

/// <summary>
/// Paged profile search across owners and dogs. Input is classified by
/// <see cref="ProfileLinkParser"/>, so pasted links, slugs, ids and free text all work
/// through the same endpoint. An empty query is browse mode: everything, alphabetically.
/// </summary>
public sealed class SearchService(AppDbContext db)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    private const string OwnerType = "Owner";
    private const string DogType = "Dog";

    public async Task<SearchResponse> SearchAsync(
        string? q,
        ProfileTypeFilter type,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);

        var parsed = ProfileLinkParser.Parse(q);
        var term = parsed.Value;

        switch (parsed.Kind)
        {
            case SearchKind.Id when Guid.TryParse(term, out var id):
                return await PageAsync(
                    Profiles(type).Where(item => item.Id == id),
                    relevance: null,
                    page, pageSize, term, "Id", cancellationToken);

            case SearchKind.Slug:
            {
                var exact = Profiles(type).Where(item => item.Slug == term || item.ShareableId == term);
                var exactPage = await PageAsync(
                    exact,
                    relevance: item => item.Slug == term,
                    page, pageSize, term, "Slug", cancellationToken);

                if (exactPage.TotalCount > 0)
                {
                    return exactPage;
                }

                // Slug-shaped input with no slug/ShareableID behind it: read the hyphens as
                // spaces and fall back to a name search.
                return await PageAsync(
                    NameSearch(Profiles(type), term.Replace('-', ' ')),
                    relevance: null,
                    page, pageSize, term, "Text", cancellationToken);
            }

            case SearchKind.Text:
                return term.Length == 0
                    ? await PageAsync(Profiles(type), null, page, pageSize, term, "Browse", cancellationToken)
                    : await PageAsync(NameSearch(Profiles(type), term), null, page, pageSize, term, "Text", cancellationToken);

            default:
                // Digits that are not a GUID: fall through to text so nothing is lost.
                return await PageAsync(NameSearch(Profiles(type), term), null, page, pageSize, term, "Text", cancellationToken);
        }
    }

    /// <summary>Owners and dogs projected into the shared result shape.</summary>
    private IQueryable<SearchResultItem> Profiles(ProfileTypeFilter type)
    {
        var owners = db.Owners.AsNoTracking().Select(o => new SearchResultItem
        {
            Type = OwnerType,
            Id = o.Id,
            Slug = o.Slug,
            ShareableId = o.ShareableID,
            Name = o.Name,
            PhotoUrl = o.ProfilePhotoUrl,
            AverageRating = (double?)o.AggregateRating,
            ReviewCount = o.ReviewCount,
            // Assigned explicitly (nulls included) so the UNION with the dog projection lines up.
            Breed = null,
            OwnerName = null,
            OwnerSlug = null
        });

        var dogs = db.Dogs.AsNoTracking().Select(d => new SearchResultItem
        {
            Type = DogType,
            Id = d.Id,
            Slug = d.Slug,
            ShareableId = d.ShareableID,
            Name = d.Name,
            Breed = d.Breed,
            PhotoUrl = d.ProfilePhotoUrl,
            OwnerName = d.Owner.Name,
            OwnerSlug = d.Owner.Slug,
            AverageRating = (double?)d.AggregateRating,
            ReviewCount = d.ReviewCount
        });

        return type switch
        {
            ProfileTypeFilter.Owners => owners,
            ProfileTypeFilter.Dogs => dogs,
            _ => owners.Concat(dogs)
        };
    }

    /// <summary>Every whitespace-separated token has to appear in the name (AND semantics).</summary>
    private static IQueryable<SearchResultItem> NameSearch(IQueryable<SearchResultItem> query, string text)
    {
        foreach (var token in text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pattern = $"%{EscapeLike(token)}%";
            query = query.Where(item => EF.Functions.ILike(item.Name, pattern));
        }

        return query;
    }

    private static async Task<SearchResponse> PageAsync(
        IQueryable<SearchResultItem> query,
        Expression<Func<SearchResultItem, bool>>? relevance,
        int page,
        int pageSize,
        string term,
        string matchType,
        CancellationToken cancellationToken)
    {
        // Deterministic ordering: relevance (if any), then name, then id — never database order.
        var ordered = relevance is null
            ? query.OrderBy(item => item.Name).ThenBy(item => item.Id)
            : query.OrderByDescending(relevance).ThenBy(item => item.Name).ThenBy(item => item.Id);

        var totalCount = await ordered.CountAsync(cancellationToken);
        var items = totalCount == 0
            ? []
            : await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new SearchResponse
        {
            Items = items.Select(ToResultItem).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Query = term,
            MatchType = matchType
        };
    }

    private static SearchResultItem ToResultItem(SearchResultItem item)
    {
        // TODO(DOG-18..20): AggregateRating/ReviewCount are still the seeder's hard-coded values
        // (they count hidden and pending reviews too). Recompute them from approved reviews once
        // the review pipeline owns these fields; until then only hide a meaningless 0.00.
        item.AverageRating = item.ReviewCount > 0 ? item.AverageRating : null;
        item.ShareableId = string.Empty;
        return item;
    }

    /// <summary>Escapes LIKE wildcards so user input cannot widen the pattern.</summary>
    private static string EscapeLike(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
