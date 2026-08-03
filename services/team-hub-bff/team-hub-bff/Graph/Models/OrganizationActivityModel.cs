namespace team_hub_bff.Graph.Models;

public sealed class OrganizationActivityPageModel
{
    public IReadOnlyList<OrganizationActivityModel> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}

public sealed class OrganizationActivityModel
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "";
    public Guid? ActorUserId { get; init; }
    public Guid? TargetUserId { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public string? Details { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}
