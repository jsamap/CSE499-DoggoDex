using System.Net;
using System.Net.Http.Json;
using DoggoDex.Shared.Profiles;
using DoggoDex.Shared.Search;

namespace DoggoDex.Web.Api;

/// <summary>
/// Real client for <c>GET /api/search</c> and <c>GET /api/profiles/{slug}</c> (DOG-23 / DOG-24).
/// Register it with <c>Api:UseMocks = false</c>; the base address comes from <c>Api:BaseUrl</c>.
/// </summary>
public sealed class HttpProfileApi(HttpClient http) : IProfileApi
{
    public async Task<SearchResponse> SearchAsync(
        string? q,
        ProfileTypeFilter type = ProfileTypeFilter.All,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/search?q={Uri.EscapeDataString(q ?? string.Empty)}"
            + $"&type={type.ToString().ToLowerInvariant()}"
            + $"&page={page}&pageSize={pageSize}";

        // TODO(auth): the API's search and profile endpoints return 401 anonymously. There is no
        // sign-in flow in this project yet (only a "Log in" link in NavMenu), so there is no token
        // to attach. Once login exists and stores the token from POST /api/auth/login
        // (DoggoDex.Shared.Auth.AuthResponse.Token), send it from here as
        // Authorization: Bearer <token> — ideally from a DelegatingHandler so every client gets it.
        var response = await http.GetFromJsonAsync<SearchResponse>(url, cancellationToken);

        return response ?? throw new InvalidOperationException("GET /api/search returned an empty body.");
    }

    public async Task<ProfileLookupResponse?> GetProfileAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var response = await http.GetAsync($"api/profiles/{Uri.EscapeDataString(slug)}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProfileLookupResponse>(cancellationToken);
    }
}
