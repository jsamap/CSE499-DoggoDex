using DoggoDex.Api.Auth;
using DoggoDex.Api.Models;
using DoggoDex.Shared.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DoggoDex.Tests.Auth;

public class TokenServiceTests
{
    private static TokenService CreateService() => new(Options.Create(new JwtOptions
    {
        Key = new string('k', 40),
        Issuer = "test-issuer",
        Audience = "test-audience",
        ExpiryMinutes = 5
    }));

    [Fact]
    public void CreateToken_ContainsUserIdAndRole()
    {
        var user = new User { Email = "a@b.com" };

        var (token, _) = CreateService().CreateToken(user, UserRole.Owner);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(nameof(UserRole.Owner), jwt.GetClaim("role").Value);
        Assert.Equal("test-issuer", jwt.Issuer);
    }

    [Fact]
    public void CreateToken_ExpiresInTheFuture()
    {
        var user = new User { Email = "a@b.com" };

        var (_, expires) = CreateService().CreateToken(user, UserRole.Establishment);

        Assert.True(expires > DateTime.UtcNow);
    }
}
