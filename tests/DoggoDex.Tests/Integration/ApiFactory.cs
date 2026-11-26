using System.Net.Http.Headers;
using System.Net.Http.Json;
using DoggoDex.Shared.Auth;
using DoggoDex.Shared.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DoggoDex.Tests.Integration;

/// <summary>
/// Boots the real API pipeline (migrations + seeder included) against the Postgres
/// instance referenced by ConnectionStrings__DefaultConnection, the same variable the
/// CI workflow exports. Integration tests no-op when that variable is absent.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string ConnectionStringVariable = "ConnectionStrings__DefaultConnection";
    private const string SigningKey = "integration-test-signing-key-0123456789abcdef";

    static ApiFactory()
    {
        // Program.cs reads configuration before the host is built, so these have to be real
        // environment variables rather than deferred in-memory providers.
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Jwt__Key")))
        {
            Environment.SetEnvironmentVariable("Jwt__Key", SigningKey);
        }
    }

    /// <summary>True when a test database is configured; otherwise integration tests no-op.</summary>
    public static bool HasDatabase =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringVariable));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(UserRole role = UserRole.Owner)
    {
        var client = CreateClient();
        var email = $"api-tests-{Guid.NewGuid():N}@doggodex.test";

        using var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Password1!",
            Role = role
        });
        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Token);

        return client;
    }
}

[CollectionDefinition(ApiCollection.Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api-integration";
}
