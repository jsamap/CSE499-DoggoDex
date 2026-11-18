namespace DoggoDex.Shared.Auth;

public static class RoleNames
{
    public const string DogOwner = "DogOwner";
    public const string Business = "Business";
    public const string Admin = "Admin";

    /// <summary>Roles a user may choose at signup. Admin is never self-assigned.</summary>
    public static readonly string[] SelfAssignable = [DogOwner, Business];

    public static readonly string[] All = [DogOwner, Business, Admin];
}