namespace DoggoDex.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "doggodex-api";
    public string Audience { get; set; } = "doggodex-web";
    public int ExpiryMinutes { get; set; } = 60;
}