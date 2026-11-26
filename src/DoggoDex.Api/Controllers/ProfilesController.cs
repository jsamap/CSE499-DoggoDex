using DoggoDex.Api.Auth;
using DoggoDex.Api.Infrastructure;
using DoggoDex.Shared.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoggoDex.Api.Controllers;

[ApiController]
[Route("api/profiles")]
public class ProfilesController(AppDbContext db) : ControllerBase
{
    private const string OwnerType = "Owner";
    private const string DogType = "Dog";

    /// <summary>
    /// Resolves a shareable profile link. Accepts the generated Slug and the legacy
    /// ShareableID, case-insensitively. A Slug match wins over a ShareableID match,
    /// and owners win over dogs, so the result is deterministic.
    /// </summary>
    [HttpGet("{slug}")]
    [Authorize(Policy = Policies.AnyUser)]
    [ProducesResponseType<ProfileLookupResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileLookupResponse>> GetBySlug(string slug)
    {
        var key = slug.Trim().ToLowerInvariant();
        if (key.Length == 0)
        {
            return NotFound();
        }

        var owner = await db.Owners.AsNoTracking()
            .Where(o => o.Slug == key || o.ShareableID.ToLower() == key)
            .OrderByDescending(o => o.Slug == key)
            .Select(o => new OwnerLookup
            {
                Id = o.Id,
                Slug = o.Slug,
                ShareableId = o.ShareableID,
                Name = o.Name,
                PhotoUrl = o.ProfilePhotoUrl,
                AverageRating = o.AggregateRating,
                ReviewCount = o.ReviewCount,
                Dogs = o.Dogs
                    .OrderBy(d => d.Name)
                    .Select(d => new ProfileDogSummary { Name = d.Name, Slug = d.Slug })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        var dog = await db.Dogs.AsNoTracking()
            .Where(d => d.Slug == key || d.ShareableID.ToLower() == key)
            .OrderByDescending(d => d.Slug == key)
            .Select(d => new DogLookup
            {
                Id = d.Id,
                Slug = d.Slug,
                ShareableId = d.ShareableID,
                Name = d.Name,
                PhotoUrl = d.ProfilePhotoUrl,
                AverageRating = d.AggregateRating,
                ReviewCount = d.ReviewCount,
                OwnerName = d.Owner.Name,
                OwnerSlug = d.Owner.Slug
            })
            .FirstOrDefaultAsync();

        if (owner is { } ownerMatch && ownerMatch.Slug == key)
        {
            return Ok(ToResponse(ownerMatch));
        }

        if (dog is { } dogMatch && dogMatch.Slug == key)
        {
            return Ok(ToResponse(dogMatch));
        }

        if (owner is not null)
        {
            return Ok(ToResponse(owner));
        }

        return dog is not null ? Ok(ToResponse(dog)) : NotFound();
    }

    private static ProfileLookupResponse ToResponse(OwnerLookup owner) => new()
    {
        Type = OwnerType,
        Id = owner.Id,
        Slug = owner.Slug,
        Name = owner.Name,
        PhotoUrl = owner.PhotoUrl,
        Dogs = owner.Dogs,
        // TODO(DOG-18..20): switch to a computed aggregate once the review pipeline owns these fields.
        AverageRating = owner.ReviewCount > 0 ? (double)owner.AverageRating : null,
        ReviewCount = owner.ReviewCount
    };

    private static ProfileLookupResponse ToResponse(DogLookup dog) => new()
    {
        Type = DogType,
        Id = dog.Id,
        Slug = dog.Slug,
        Name = dog.Name,
        PhotoUrl = dog.PhotoUrl,
        OwnerName = dog.OwnerName,
        OwnerSlug = dog.OwnerSlug,
        // TODO(DOG-18..20): switch to a computed aggregate once the review pipeline owns these fields.
        AverageRating = dog.ReviewCount > 0 ? (double)dog.AverageRating : null,
        ReviewCount = dog.ReviewCount
    };

    private sealed class OwnerLookup
    {
        public Guid Id { get; init; }
        public string Slug { get; init; } = "";
        public string ShareableId { get; init; } = "";
        public string Name { get; init; } = "";
        public string? PhotoUrl { get; init; }
        public decimal AverageRating { get; init; }
        public int ReviewCount { get; init; }
        public List<ProfileDogSummary> Dogs { get; init; } = [];
    }

    private sealed class DogLookup
    {
        public Guid Id { get; init; }
        public string Slug { get; init; } = "";
        public string ShareableId { get; init; } = "";
        public string Name { get; init; } = "";
        public string? PhotoUrl { get; init; }
        public decimal AverageRating { get; init; }
        public int ReviewCount { get; init; }
        public string OwnerName { get; init; } = "";
        public string OwnerSlug { get; init; } = "";
    }
}
