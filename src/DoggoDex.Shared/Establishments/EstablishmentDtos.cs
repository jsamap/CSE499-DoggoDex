using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DoggoDex.Shared.Establishments;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EstablishmentType
{
    Cafe,
    Restaurant,
    Hotel,
    Rental,
    Store,
    Other
}

public class EstablishmentUpsertRequest
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    public EstablishmentType Type { get; set; } = EstablishmentType.Other;

    [MaxLength(200)]
    public string? Location { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
}

public class EstablishmentResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public EstablishmentType Type { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}