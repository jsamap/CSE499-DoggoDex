using System.ComponentModel.DataAnnotations;
using DoggoDex.Shared.Models;

namespace DoggoDex.Shared.Auth;

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = "";

    [Required, MinLength(8), MaxLength(100)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*\d).{8,100}$",
        ErrorMessage = "Password must be 8-100 characters and contain a lowercase letter and a digit.")]
    public string Password { get; set; } = "";

    /// <summary>Owner, Establishment or Community. Admin cannot be self-assigned.</summary>
    [Required]
    public UserRole Role { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}

public class AuthResponse
{
    public string Token { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public UserInfo User { get; set; } = new();
}

public class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public UserRole Role { get; set; }
}
