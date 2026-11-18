using System.Text.Json.Serialization;

namespace DoggoDex.Shared.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
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