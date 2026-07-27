namespace team_hub_bff.Graph.Models;

public sealed class OrganizationMemberModel
{
    public Guid UserId { get; init; }
    public DateTimeOffset JoinedAt { get; init; }
    public IReadOnlyList<Guid> TeamIds { get; init; } = [];
    public IReadOnlyList<RoleModel> Roles { get; init; } = [];
}

public sealed class RoleModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Scope { get; init; } = "";
    public bool IsSystem { get; init; }
}

public sealed class UserModel
{
    public Guid Id { get; init; }
    public string Username { get; init; } = "";
    public string Name { get; init; } = "";
    public string Surname { get; init; } = "";
}
