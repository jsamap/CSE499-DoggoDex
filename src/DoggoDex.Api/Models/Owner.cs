namespace DoggoDex.Api.Models;

public class Owner
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public decimal AggregateRating { get; set; } = 0.00m;
    public int ReviewCount { get; set; } = 0;
    public string ShareableID { get; set; } = Guid.NewGuid().ToString("N");

    public User User { get; set; } = null!;
    public ICollection<Dog> Dogs { get; set; } = new List<Dog>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}