using System.Text.Json.Serialization;
using DoggoDex.Shared.Common;

namespace DoggoDex.Shared.Search;

/// <summary>Which profile types a search should cover.</summary>
public enum ProfileTypeFilter
{
    All,
    Owners,
    Dogs
}

/// <summary>One row of a profile search result.</summary>
public class SearchResultItem
{
    /// <summary>"Owner" or "Dog".</summary>
    public string Type { get; set; } = "";

    public Guid Id { get; set; }

    public string Slug { get; set; } = "";

    public string Name { get; set; } = "";

    /// <summary>Set for dogs only.</summary>
    public string? Breed { get; set; }

    public string? PhotoUrl { get; set; }

    /// <summary>Set for dogs only.</summary>
    public string? OwnerName { get; set; }

    /// <summary>Set for dogs only.</summary>
    public string? OwnerSlug { get; set; }

    /// <summary>Null while the profile has no reviews.</summary>
    public double? AverageRating { get; set; }

    public int ReviewCount { get; set; }

    /// <summary>Matching only; never serialized.</summary>
    [JsonIgnore]
    public string ShareableId { get; set; } = "";
}

public class SearchResponse : PagedResult<SearchResultItem>
{
    /// <summary>The trimmed query that was executed ("" in browse mode).</summary>
    public string Query { get; set; } = "";

    /// <summary>Browse, Text, Id or Slug — how the input was interpreted.</summary>
    public string MatchType { get; set; } = "";
}
