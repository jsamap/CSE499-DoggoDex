namespace DoggoDex.Api.Auth;

public static class Policies
{
    public const string OwnerOnly = "OwnerOnly";
    public const string BusinessOnly = "BusinessOnly";
    public const string AnyUser = "AnyUser";
}