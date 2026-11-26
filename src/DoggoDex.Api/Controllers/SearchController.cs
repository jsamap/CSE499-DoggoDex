using DoggoDex.Api.Auth;
using DoggoDex.Api.Services;
using DoggoDex.Shared.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoggoDex.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController(SearchService search) : ControllerBase
{
    /// <summary>
    /// Paged profile search over owners and dogs. Accepts free text, a shareable slug, a
    /// pasted profile link or a profile id; an empty query lists everything alphabetically.
    /// </summary>
    /// <param name="q">Free text, slug, link or id. Omit or leave empty to browse.</param>
    /// <param name="type">all (default), owners or dogs.</param>
    /// <param name="page">1-based page number; values below 1 are treated as 1.</param>
    /// <param name="pageSize">1-50, defaults to 20.</param>
    [HttpGet]
    [Authorize(Policy = Policies.AnyUser)]
    [ProducesResponseType<SearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SearchResponse>> Search(
        [FromQuery] string? q,
        [FromQuery] ProfileTypeFilter type = ProfileTypeFilter.All,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = SearchService.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var response = await search.SearchAsync(q, type, page, pageSize, cancellationToken);
        return Ok(response);
    }
}
