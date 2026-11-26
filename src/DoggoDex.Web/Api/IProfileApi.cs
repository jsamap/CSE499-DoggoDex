using DoggoDex.Shared.Profiles;
using DoggoDex.Shared.Search;

namespace DoggoDex.Web.Api;

/// <summary>
/// The two profile endpoints the UI needs, expressed with the shared DTOs so the mock and the
/// real client are interchangeable: <c>GET /api/search</c> and <c>GET /api/profiles/{slug}</c>.
/// Both require a bearer token (any role) on the real API.
/// </summary>
public interface IProfileApi
{
    /// <param name="q">Free text, profile slug, pasted link or profile id. Empty = browse mode.</param>
    /// <param name="type">Which profiles to include; defaults to everything.</param>
    /// <param name="page">1-based page number; values below 1 are treated as 1.</param>
    /// <param name="pageSize">
    /// 1-50; 0 or negative means 20. The server clamps silently, so always read
    /// <see cref="SearchResponse.PageSize"/> from the response instead of echoing the request.
    /// </param>
    Task<SearchResponse> SearchAsync(
        string? q,
        ProfileTypeFilter type = ProfileTypeFilter.All,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a generated slug or a legacy shareable id, case-insensitively.
    /// </summary>
    /// <returns>Null when nothing matches (the API's 404).</returns>
    Task<ProfileLookupResponse?> GetProfileAsync(string slug, CancellationToken cancellationToken = default);
}
