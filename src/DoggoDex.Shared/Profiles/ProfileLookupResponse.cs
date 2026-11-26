namespace DoggoDex.Shared.Profiles;

/// <summary>
/// Public profile payload for GET /api/profiles/{slug}. Never contains e-mail
/// addresses or other contact details.
/// </summary>
public class ProfileLookupResponse
{
    /// <summary>"Owner" or "Dog".</summary>
    public string Type { get; set; } = "";

    public Guid Id { get; set; }

    public string Slug { get; set; } = "";

    public string Name { get; set; } = "";

    public string? PhotoUrl { get; set; }

    /// <summary>Set for dogs only.</summary>
    public string? OwnerName { get; set; }

    /// <summary>Set for dogs only.</summary>
    public string? OwnerSlug { get; set; }

    /// <summary>Set for owners only.</summary>
    public List<ProfileDogSummary> Dogs { get; set; } = [];

    /// <summary>Null while the profile has no reviews.</summary>
    public double? AverageRating { get; set; }

    public int ReviewCount { get; set; }
}

public class ProfileDogSummary
{
    public string Name { get; set; } = "";

    public string Slug { get; set; } = "";
}
