namespace DoggoDex.Shared.Models;

public enum UserRole
{
    Owner,
    Establishment,
    Community,
    Admin
}

public enum ReviewerType
{
    Owner,
    Business,
    Community
}

public enum ModerationStatus
{
    Pending,
    Approved,
    Hidden
}