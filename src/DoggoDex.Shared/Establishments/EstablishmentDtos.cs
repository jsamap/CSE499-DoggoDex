using System.ComponentModel.DataAnnotations;

namespace DoggoDex.Shared.Establishments;

public class EstablishmentUpsertRequest
{
    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string? PetPolicy { get; set; }

    [MaxLength(1000)]
    public string? PerksDescription { get; set; }

    [MaxLength(500)]
    public string? ProfilePhotoUrl { get; set; }
}

public class EstablishmentResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public string? PetPolicy { get; set; }
    public string? PerksDescription { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}
