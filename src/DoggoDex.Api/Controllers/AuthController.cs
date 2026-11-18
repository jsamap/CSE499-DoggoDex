using DoggoDex.Api.Auth;
using DoggoDex.Api.Infrastructure;
using DoggoDex.Api.Models;
using DoggoDex.Shared.Auth;
using DoggoDex.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DoggoDex.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, IPasswordHasher<User> hasher, ITokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (request.Role == UserRole.Admin)
        {
            ModelState.AddModelError(nameof(request.Role), "Admin accounts cannot be self-registered.");
            return ValidationProblem(ModelState);
        }

        var email = NormalizeEmail(request.Email);

        if (await db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new ProblemDetails { Title = "That email is already registered." });

        var user = new User
        {
            Email = email,
            Role = request.Role
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Ok(BuildResponse(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var invalid = new ProblemDetails { Title = "Invalid email or password." };
        var email = NormalizeEmail(request.Email);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null)
            return Unauthorized(invalid);

        var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
            return Unauthorized(invalid);

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync();
        }

        return Ok(BuildResponse(user));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> Me()
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == User.GetUserId());

        return user is null ? Unauthorized() : Ok(ToInfo(user));
    }

    private AuthResponse BuildResponse(User user)
    {
        var (token, expires) = tokens.CreateToken(user, user.Role);
        return new AuthResponse { Token = token, ExpiresAtUtc = expires, User = ToInfo(user) };
    }

    private static UserInfo ToInfo(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Role = user.Role
    };

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
