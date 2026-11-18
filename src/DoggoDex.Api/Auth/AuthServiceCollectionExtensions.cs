using System.Text;
using DoggoDex.Api.Models;
using DoggoDex.Shared.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace DoggoDex.Api.Auth;

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddDoggoDexAuth(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<JwtOptions>()
            .Bind(config.GetSection(JwtOptions.SectionName))
            .Validate(o => Encoding.UTF8.GetByteCount(o.Key ?? "") >= 32,
                "Jwt:Key must be at least 32 characters.")
            .ValidateOnStart();

        var jwt = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        // Users live in the shared AppDbContext (Models.User); only Identity's password
        // hashing primitive is used, so no Identity stores or extra tables are required.
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<ITokenService, TokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.OwnerOnly, p => p.RequireRole(nameof(UserRole.Owner)))
            .AddPolicy(Policies.BusinessOnly, p => p.RequireRole(nameof(UserRole.Establishment)))
            .AddPolicy(Policies.AnyUser, p => p.RequireAuthenticatedUser());

        return services;
    }
}
