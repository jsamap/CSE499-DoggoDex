using DoggoDex.Shared.Auth;
using Microsoft.AspNetCore.Identity;

namespace DoggoDex.Api.Auth;

public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var name in RoleNames.All)
        {
            if (!await roles.RoleExistsAsync(name))
                await roles.CreateAsync(new IdentityRole<Guid>(name));
        }
    }
}