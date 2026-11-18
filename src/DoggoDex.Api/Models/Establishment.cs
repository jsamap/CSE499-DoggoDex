namespace DoggoDex.Api.Models;

public class Establishment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PetPolicy { get; set; }
    public string? PerksDescription { get; set; }
    public string? ProfilePhotoUrl { get; set; }

    public User User { get; set; } = null!;
}