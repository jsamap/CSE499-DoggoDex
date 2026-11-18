using System.Security.Claims;
using System.Text;
using DoggoDex.Api.Models;
using DoggoDex.Shared.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DoggoDex.Api.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user, UserRole role);
}

public class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    public (string Token, DateTime ExpiresAtUtc) CreateToken(User user, UserRole role)
    {
        var o = options.Value;
        var expires = DateTime.UtcNow.AddMinutes(o.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", user.Id.ToString()),
                new Claim("email", user.Email ?? ""),
                new Claim("name", user.Email ?? ""),
                new Claim("role", role.ToString()),
                new Claim("jti", Guid.NewGuid().ToString())
            ]),
            Issuer = o.Issuer,
            Audience = o.Audience,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
