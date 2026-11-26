namespace DoggoDex.Web.Api;

/// <summary>
/// One row of the in-memory profile set. Shaped like <c>SearchResultItem</c> plus the legacy
/// shareable id, which the API uses for matching but never serializes.
/// </summary>
public sealed record MockProfile
{
    public required string Type { get; init; }

    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    /// <summary>Dogs only.</summary>
    public string? Breed { get; init; }

    public string? PhotoUrl { get; init; }

    /// <summary>Dogs only.</summary>
    public string? OwnerName { get; init; }

    /// <summary>Dogs only.</summary>
    public string? OwnerSlug { get; init; }

    /// <summary>Null while the profile has no reviews — the UI must not render 0.0 stars.</summary>
    public double? AverageRating { get; init; }

    public int ReviewCount { get; init; }

    /// <summary>Legacy link id (e.g. "jared-samayoa"); still resolvable through the API.</summary>
    public string ShareableId { get; init; } = "";
}

/// <summary>
/// Stand-in data for develop-against-mocks work. Ids and slugs for Jared, Camila, Sky and Luna
/// were copied from a live seeded database so shared links keep working; the remaining rows are
/// stable mock values. Ratings are seeder-provided on the API too (DOG-18..20 will compute them
/// from reviews), so numbers will not match the review lists.
/// </summary>
public static class MockProfileData
{
    public const string OwnerType = "Owner";
    public const string DogType = "Dog";

    public static IReadOnlyList<MockProfile> Profiles { get; } =
    [
        new()
        {
            Type = OwnerType,
            Id = Guid.Parse("8a2d829b-1752-41b8-812f-b09ee5138378"),
            Slug = "jared-samayoa-vuq3b",
            Name = "Jared Samayoa",
            PhotoUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb",
            AverageRating = 4.9,
            ReviewCount = 2,
            ShareableId = "jared-samayoa"
        },
        new()
        {
            Type = OwnerType,
            Id = Guid.Parse("0a27f496-8d7a-4723-b1a9-667156b6294a"),
            Slug = "camila-reyes-vvk7c",
            Name = "Camila Reyes",
            PhotoUrl = "https://images.unsplash.com/photo-1517841905240-472988babdf9",
            AverageRating = 3.6,
            ReviewCount = 1,
            ShareableId = "camila-reyes"
        },
        new()
        {
            Type = OwnerType,
            Id = Guid.Parse("c4e1a7d3-5b62-4f80-9a17-2e6d8f0b3c41"),
            Slug = "priya-nair-mock1",
            Name = "Priya Nair",
            PhotoUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330",
            AverageRating = null,
            ReviewCount = 0,
            ShareableId = "priya-nair"
        },
        new()
        {
            Type = DogType,
            Id = Guid.Parse("2beb0f29-163f-4c37-8448-f82e4f3d026f"),
            Slug = "sky-5jbsr",
            Name = "Sky",
            Breed = "Siberian Husky",
            PhotoUrl = "https://images.unsplash.com/photo-1605568427561-40dd23c2bbe4",
            OwnerName = "Jared Samayoa",
            OwnerSlug = "jared-samayoa-vuq3b",
            AverageRating = 4.9,
            ReviewCount = 2,
            ShareableId = "sky-husky"
        },
        new()
        {
            Type = DogType,
            Id = Guid.Parse("87b5aa5f-935b-4c20-abc0-2859d140d9f6"),
            Slug = "luna-p54ap",
            Name = "Luna",
            Breed = "Beagle",
            PhotoUrl = "https://images.unsplash.com/photo-1537151608828-ea2b11777ee8",
            OwnerName = "Camila Reyes",
            OwnerSlug = "camila-reyes-vvk7c",
            AverageRating = 2.8,
            ReviewCount = 1,
            ShareableId = "luna-beagle"
        },
        new()
        {
            Type = DogType,
            Id = Guid.Parse("1d7b9a54-6c02-4b8e-8f31-7a90c5e2b6d8"),
            Slug = "max-golden-x7k2m",
            Name = "Max",
            Breed = "Golden Retriever",
            PhotoUrl = "https://images.unsplash.com/photo-1552053831-71594a27632d",
            OwnerName = "Camila Reyes",
            OwnerSlug = "camila-reyes-vvk7c",
            AverageRating = 4.6,
            ReviewCount = 1,
            ShareableId = "max-golden"
        },
        new()
        {
            Type = DogType,
            Id = Guid.Parse("9f2c6d18-3a75-4e09-b6c4-5d18e7a03b2f"),
            Slug = "mochi-shiba-mock2",
            Name = "Mochi",
            Breed = "Shiba Inu",
            PhotoUrl = "https://images.unsplash.com/photo-1583511655857-d19b40a7a54e",
            OwnerName = "Priya Nair",
            OwnerSlug = "priya-nair-mock1",
            AverageRating = null,
            ReviewCount = 0,
            ShareableId = "mochi-shiba"
        }
    ];
}
