using DoggoDex.Shared.Establishments;

namespace DoggoDex.Api.Domain;

public class Establishment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerUserId { get; set; }
    public ApplicationUser? OwnerUser { get; set; }

    public string Name { get; set; } = "";
    public EstablishmentType Type { get; set; } = EstablishmentType.Other;
    public string? Location { get; set; }
    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}