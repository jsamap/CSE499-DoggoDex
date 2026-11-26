using System.Net;
using System.Net.Http.Json;
using DoggoDex.Api.Infrastructure;
using DoggoDex.Api.Models;
using DoggoDex.Shared.Models;
using DoggoDex.Shared.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DoggoDex.Tests.Integration;

[Collection(ApiCollection.Name)]
public class ProfilesApiTests(ApiFactory factory)
{
    private const string SeededOwnerShareableId = "jared-samayoa";

    [Fact]
    public async Task GetBySlug_ReturnsOwnerProfileWithDogs()
    {
        if (!ApiFactory.HasDatabase)
        {
            return; // Requires Postgres; skipped without ConnectionStrings__DefaultConnection.
        }

        using var client = await factory.CreateAuthenticatedClientAsync();
        var (owner, dog) = await SeedOwnerWithDogAsync();

        using var response = await client.GetAsync($"/api/profiles/{owner.Slug}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("@", raw);

        var body = await response.Content.ReadFromJsonAsync<ProfileLookupResponse>();
        Assert.NotNull(body);
        Assert.Equal("Owner", body!.Type);
        Assert.Equal(owner.Id, body.Id);
        Assert.Equal(owner.Slug, body.Slug);
        Assert.Equal(owner.Name, body.Name);
        Assert.Null(body.OwnerName);
        Assert.Null(body.OwnerSlug);
        Assert.Null(body.AverageRating);
        Assert.Equal(0, body.ReviewCount);
        Assert.Contains(body.Dogs, summary => summary.Slug == dog.Slug && summary.Name == dog.Name);
    }

    [Fact]
    public async Task GetBySlug_ReturnsDogProfileWithOwnerLink()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();
        var (owner, dog) = await SeedOwnerWithDogAsync();

        var body = await client.GetFromJsonAsync<ProfileLookupResponse>($"/api/profiles/{dog.Slug}");

        Assert.NotNull(body);
        Assert.Equal("Dog", body!.Type);
        Assert.Equal(dog.Id, body.Id);
        Assert.Equal(dog.Slug, body.Slug);
        Assert.Equal(owner.Name, body.OwnerName);
        Assert.Equal(owner.Slug, body.OwnerSlug);
        Assert.Empty(body.Dogs);
        Assert.Null(body.AverageRating);
    }

    [Fact]
    public async Task GetBySlug_IsCaseInsensitive()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();
        var (owner, _) = await SeedOwnerWithDogAsync();

        var body = await client.GetFromJsonAsync<ProfileLookupResponse>(
            $"/api/profiles/{owner.Slug.ToUpperInvariant()}");

        Assert.NotNull(body);
        Assert.Equal(owner.Id, body!.Id);
    }

    [Fact]
    public async Task GetBySlug_AlsoResolvesTheLegacyShareableId()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();
        var (owner, _) = await SeedOwnerWithDogAsync();

        using var response = await client.GetAsync($"/api/profiles/{owner.ShareableID}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProfileLookupResponse>();
        Assert.NotNull(body);
        Assert.Equal("Owner", body!.Type);
        Assert.Equal(owner.Id, body.Id);
        // Slug stays canonical; the ShareableID is deliberately not copied into it.
        Assert.Equal(owner.Slug, body.Slug);
        Assert.NotEqual(owner.ShareableID, body.Slug);
    }

    [Fact]
    public async Task GetBySlug_ResolvesTheSeededJaredSamayoaShareableId()
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

        using var response = await client.GetAsync($"/api/profiles/{SeededOwnerShareableId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProfileLookupResponse>();
        Assert.NotNull(body);
        Assert.Equal("Owner", body!.Type);
        Assert.Equal(seeded!.Id, body.Id);
        Assert.Equal(seeded.Slug, body.Slug);
        Assert.StartsWith("jared-samayoa", body.Slug);
        Assert.NotEqual(SeededOwnerShareableId, body.Slug);
    }

    [Fact]
    public async Task GetBySlug_ReturnsNotFoundForUnknownSlug()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var client = await factory.CreateAuthenticatedClientAsync();

        using var response = await client.GetAsync($"/api/profiles/nobody-here-{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBySlug_RequiresAuthentication()
    {
        if (!ApiFactory.HasDatabase)
        {
            return;
        }

        using var anonymous = factory.CreateClient();

        using var response = await anonymous.GetAsync($"/api/profiles/{SeededOwnerShareableId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<(Owner Owner, Dog Dog)> SeedOwnerWithDogAsync()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            Email = $"owner-{marker}@doggodex.test",
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
            Bio = "Created by ProfilesApiTests."
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

        // Slugs are assigned by SlugSaveChangesInterceptor, never by the caller.
        Assert.False(string.IsNullOrWhiteSpace(owner.Slug));
        Assert.False(string.IsNullOrWhiteSpace(dog.Slug));

        return (owner, dog);
    }
}
