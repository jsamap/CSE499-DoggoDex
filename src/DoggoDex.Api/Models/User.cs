using DoggoDex.Shared.Models;

namespace DoggoDex.Api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Owner;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    
    public Owner? OwnerProfile { get; set; }
    public Establishment? EstablishmentProfile { get; set; }
    public ICollection<Review> AuthoredReviews { get; set; } = new List<Review>();
}