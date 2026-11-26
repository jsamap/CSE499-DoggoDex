using System.Net;
using System.Net.Http.Json;
using DoggoDex.Api.Infrastructure;
using DoggoDex.Api.Models;
using DoggoDex.Api.Services;
using DoggoDex.Shared.Models;
using DoggoDex.Shared.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoggoDex.Tests.Integration;

[Collection(ApiCollection.Name)]
public class SearchApiTests(ApiFactory factory)
{
    private const string SeededOwnerShareableId = "jared-samayoa";

    [Fact]
    public async Task Search_BrowseMode_ListsProfilesAlphabetically()
    {
        if (!ApiFactory.HasDatabase)
        {
            return; // Requires Postgres; skipped without ConnectionStrings__DefaultConnection.
        }

        using var client = await factory.CreateAuthenticatedClientAsync();

        var body = await client.GetFromJsonAsync<SearchResponse>("/api/search?pageSize=50");

        Assert.NotNull(body);
        Assert.Equal("Browse", body!.MatchType);
        Assert.Equal(string.Empty, body.Query);
        Assert.NotEmpty(body.Items);
        Assert.Equal(SearchService.MaxPageSize, body.PageSize);
        Assert.Equal(Math.Min(SearchService.MaxPageSize, body.TotalCount), body.Items.Count);

        // Name ascending, then Id — never database order.
        var ordered = body.Items
            .OrderBy(item => item.Name, StringComparer.Ordinal)
            .ThenBy(item => item.Id)
            .Select(item => item.Id);
        Assert.Equal(ordered, body.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task Search_TextQuery_MatchesNameTokens()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();
        var (owner, _) = await SeedOwnerWithDogAsync();

        var body = await client.GetFromJsonAsync<SearchResponse>($"/api/search?q={owner.Name}");

        Assert.NotNull(body);
        Assert.Equal("Text", body!.MatchType);
        Assert.Contains(body.Items, item => item.Id == owner.Id && item.Type == "Owner");
        Assert.All(body.Items, item => Assert.Contains(owner.Name, item.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Search_MultiWordText_RequiresEveryToken()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();

        var body = await client.GetFromJsonAsync<SearchResponse>("/api/search?q=jared%20samayoa");

        Assert.NotNull(body);
        Assert.Equal("Text", body!.MatchType);
        Assert.Contains(body.Items, item => item.Name == "Jared Samayoa" && item.Type == "Owner");
        Assert.All(body.Items, item =>
        {
            Assert.Contains("jared", item.Name, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samayoa", item.Name, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Search_SlugQuery_ResolvesExactSlugOnly()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();
        var (owner, _) = await SeedOwnerWithDogAsync();

        var body = await client.GetFromJsonAsync<SearchResponse>($"/api/search?q={owner.Slug}");

        Assert.NotNull(body);
        Assert.Equal("Slug", body!.MatchType);
        Assert.Equal(1, body.TotalCount);
        Assert.Equal(owner.Id, Assert.Single(body.Items).Id);
    }

    [Fact]
    public async Task Search_LegacyShareableId_StillResolvesTheProfile()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeded = await db.Owners.AsNoTracking()
            .FirstOrDefaultAsync(o => o.ShareableID == SeededOwnerShareableId);

        Assert.NotNull(seeded);

        var body = await client.GetFromJsonAsync<SearchResponse>($"/api/search?q={SeededOwnerShareableId}");

        Assert.NotNull(body);
        Assert.Equal("Slug", body!.MatchType);
        Assert.Equal(1, body.TotalCount);

        var match = Assert.Single(body.Items);
        Assert.Equal(seeded!.Id, match.Id);
        Assert.Equal("Owner", match.Type);
        Assert.Equal(seeded.Slug, match.Slug);
        Assert.NotEqual(SeededOwnerShareableId, match.Slug);
    }

    [Fact]
    public async Task Search_IdQuery_ResolvesExactlyThatProfile()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();
        var (owner, dog) = await SeedOwnerWithDogAsync();

        var body = await client.GetFromJsonAsync<SearchResponse>($"/api/search?q={dog.Id}");

        Assert.NotNull(body);
        Assert.Equal("Id", body!.MatchType);
        Assert.Equal(1, body.TotalCount);

        var match = Assert.Single(body.Items);
        Assert.Equal(dog.Id, match.Id);
        Assert.Equal("Dog", match.Type);
        Assert.Equal(owner.Name, match.OwnerName);
        Assert.Equal(owner.Slug, match.OwnerSlug);
    }

    [Fact]
    public async Task Search_SlugShapedInputWithoutAMatch_FallsBackToNameSearch()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();
        var (owner, dog) = await SeedOwnerWithDogAsync();

        // No profile owns this slug, so the hyphens are read as spaces.
        var body = await client.GetFromJsonAsync<SearchResponse>($"/api/search?q={owner.Slug}-no-such-suffix");

        Assert.NotNull(body);
        Assert.Equal("Text", body!.MatchType);
        Assert.DoesNotContain(body.Items, item => item.Id == owner.Id);
        Assert.DoesNotContain(body.Items, item => item.Id == dog.Id);
    }

    [Fact]
    public async Task Search_FallsBackToNameSearchForUnmatchedSlug()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();
        await SeedOwnerWithDogAsync();

        var body = await client.GetFromJsonAsync<SearchResponse>("/api/search?q=zed-probe-dog");

        Assert.NotNull(body);
        Assert.Equal("Text", body!.MatchType);
        Assert.NotEmpty(body.Items);
        Assert.All(body.Items, item => Assert.Equal("Dog", item.Type));
        Assert.All(body.Items, item => Assert.Contains("zed probe dog", item.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Search_TypeFilter_RestrictsToDogs()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();

        var body = await client.GetFromJsonAsync<SearchResponse>("/api/search?q=sky&type=dogs");

        Assert.NotNull(body);
        Assert.NotEmpty(body.Items);
        Assert.All(body.Items, item => Assert.Equal("Dog", item.Type));
        Assert.Contains(body.Items, item => item.Name == "Sky");
    }

    [Fact]
    public async Task Search_Paging_SplitsResultsAndReportsTheEnvelope()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();
        var first = await client.GetFromJsonAsync<SearchResponse>("/api/search?page=1&pageSize=2");
        var second = await client.GetFromJsonAsync<SearchResponse>("/api/search?page=2&pageSize=2");

        Assert.NotNull(first);
        Assert.NotNull(second);

        Assert.Equal(2, first!.PageSize);
        Assert.Equal(1, first.Page);
        Assert.False(first.HasPreviousPage);
        Assert.Equal((int)Math.Ceiling(first.TotalCount / 2d), first.TotalPages);
        Assert.Equal(Math.Min(2, first.TotalCount), first.Items.Count);

        if (first.TotalCount <= 2)
        {
            return;
        }

        Assert.Equal(2, second!.Page);
        Assert.True(second.HasPreviousPage);
        Assert.Equal(first.TotalCount, second.TotalCount);
        Assert.Equal(second.Page < second.TotalPages, second.HasNextPage);
        Assert.Empty(first.Items.Select(item => item.Id).Intersect(second.Items.Select(item => item.Id)));
    }

    [Fact]
    public async Task Search_ClampsPagingArguments()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();

        var oversized = await client.GetFromJsonAsync<SearchResponse>("/api/search?page=0&pageSize=1000");
        var zeroSized = await client.GetFromJsonAsync<SearchResponse>("/api/search?pageSize=0");

        Assert.NotNull(oversized);
        Assert.NotNull(zeroSized);

        Assert.Equal(1, oversized!.Page);
        Assert.Equal(SearchService.MaxPageSize, oversized.PageSize);
        Assert.False(oversized.HasPreviousPage);

        Assert.Equal(SearchService.DefaultPageSize, zeroSized!.PageSize);
    }

    [Fact]
    public async Task Search_HidesTheRatingWhenThereAreNoReviews()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();
        var (owner, _) = await SeedOwnerWithDogAsync();

        var body = await client.GetFromJsonAsync<SearchResponse>($"/api/search?q={owner.Name}");

        Assert.NotNull(body);
        var match = Assert.Single(body!.Items, item => item.Id == owner.Id);
        Assert.Equal(0, match.ReviewCount);
        Assert.Null(match.AverageRating);
    }

    [Fact]
    public async Task Search_EscapesLikeWildcards()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();

        var percent = await client.GetFromJsonAsync<SearchResponse>("/api/search?q=%25");
        var underscore = await client.GetFromJsonAsync<SearchResponse>("/api/search?q=zed_probe");

        Assert.NotNull(percent);
        Assert.NotNull(underscore);

        // '%' and '_' must be matched literally, not as wildcards.
        Assert.Equal(0, percent!.TotalCount);
        Assert.Equal(0, underscore!.TotalCount);
    }

    [Fact]
    public async Task Search_DoesNotExposeInternalIdentifiers()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync("/api/search?pageSize=1");
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("shareableId", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@", raw);
    }

    [Fact]
    public async Task Search_RequiresAuthentication()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var anonymous = factory.CreateClient();

        using var response = await anonymous.GetAsync("/api/search?q=sky");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<(Owner Owner, Dog Dog)> SeedOwnerWithDogAsync()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Email = $"search-{marker}@doggodex.test",
            PasswordHash = "not-used-by-these-tests",
            Role = UserRole.Owner,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var owner = new Owner
        {
            UserId = user.Id,
            Name = $"Zed Probe Owner {marker}",
            Bio = "Created by SearchApiTests."
        };
        var dog = new Dog
        {
            OwnerId = owner.Id,
            Name = $"Zed Probe Dog {marker}",
            Breed = "Test Hound"
        };
        db.Owners.Add(owner);
        db.Dogs.Add(dog);
        await db.SaveChangesAsync();

        return (owner, dog);
    }
}
