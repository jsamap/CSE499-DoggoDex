using DoggoDex.Shared.Models;

namespace DoggoDex.Api.Models;

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReviewerId { get; set; }
    public Guid? OwnerId { get; set; }
    public Guid? DogId { get; set; }

    public int LeashManners { get; set; }
    public int NoiseControl { get; set; }
    public int SocialReactivity { get; set; }
    public int OwnerAttentiveness { get; set; }
    public int Cleanliness { get; set; }

    public string? WrittenFeedback { get; set; }
    public ReviewerType ReviewerType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Approved;

    public User Reviewer { get; set; } = null!;
    public Owner? Owner { get; set; }
    public Dog? Dog { get; set; }
}