namespace DoggoDex.Api.Models;

public class Dog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public int? Age { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public decimal AggregateRating { get; set; } = 0.00m;
    public int ReviewCount { get; set; } = 0;
    public string ShareableID { get; set; } = Guid.NewGuid().ToString("N");


    public Owner Owner { get; set; } = null!;
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}