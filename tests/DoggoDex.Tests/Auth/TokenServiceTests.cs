using DoggoDex.Api.Auth;
using DoggoDex.Api.Domain;
using DoggoDex.Shared.Auth;
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
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "a@b.com", DisplayName = "A" };

        var (token, _) = CreateService().CreateToken(user, RoleNames.DogOwner);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(RoleNames.DogOwner, jwt.GetClaim("role").Value);
        Assert.Equal("test-issuer", jwt.Issuer);
    }

    [Fact]
    public void CreateToken_ExpiresInTheFuture()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "a@b.com", DisplayName = "A" };

        var (_, expires) = CreateService().CreateToken(user, RoleNames.Business);

        Assert.True(expires > DateTime.UtcNow);
    }
}