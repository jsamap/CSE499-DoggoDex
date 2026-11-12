using Npgsql;

namespace DoggoDex.Api.Infrastructure;

public static class DatabaseUrl
{
    /// <summary>
    /// Accepts either a normal Npgsql connection string or a postgres:// URL
    /// (the format Render provides) and returns an Npgsql connection string.
    /// </summary>
    public static string ToNpgsql(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = SslMode.Prefer
        };

        return builder.ConnectionString;
    }
}
