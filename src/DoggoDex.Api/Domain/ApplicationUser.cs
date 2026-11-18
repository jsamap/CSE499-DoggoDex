using Microsoft.AspNetCore.Identity;

namespace DoggoDex.Api.Domain;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}