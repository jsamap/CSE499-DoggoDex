using DoggoDex.Api.Auth;
using DoggoDex.Api.Domain;
using DoggoDex.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DoggoDex.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<ApplicationUser> users, ITokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (!RoleNames.SelfAssignable.Contains(request.Role))
        {
            ModelState.AddModelError(nameof(request.Role), "Role must be DogOwner or Business.");
            return ValidationProblem(ModelState);
        }

        if (await users.FindByEmailAsync(request.Email) is not null)
            return Conflict(new ProblemDetails { Title = "That email is already registered." });

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName.Trim()
        };

        var create = await users.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            foreach (var error in create.Errors)
                ModelState.AddModelError(error.Code, error.Description);
            return ValidationProblem(ModelState);
        }

        await users.AddToRoleAsync(user, request.Role);
        return Ok(BuildResponse(user, request.Role));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var invalid = new ProblemDetails { Title = "Invalid email or password." };

        var user = await users.FindByEmailAsync(request.Email);
        if (user is null)
            return Unauthorized(invalid);

        if (await users.IsLockedOutAsync(user))
            return StatusCode(StatusCodes.Status423Locked,
                new ProblemDetails { Title = "Account temporarily locked. Try again later." });

        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);
            return Unauthorized(invalid);
        }

        await users.ResetAccessFailedCountAsync(user);

        var role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? RoleNames.DogOwner;
        return Ok(BuildResponse(user, role));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> Me()
    {
        var user = await users.FindByIdAsync(User.GetUserId().ToString());
        if (user is null)
            return Unauthorized();

        var role = (await users.GetRolesAsync(user)).FirstOrDefault() ?? "";
        return Ok(ToInfo(user, role));
    }

    private AuthResponse BuildResponse(ApplicationUser user, string role)
    {
        var (token, expires) = tokens.CreateToken(user, role);
        return new AuthResponse { Token = token, ExpiresAtUtc = expires, User = ToInfo(user, role) };
    }

    private static UserInfo ToInfo(ApplicationUser user, string role) => new()
    {
        Id = user.Id,
        Email = user.Email ?? "",
        DisplayName = user.DisplayName,
        Role = role
    };
}