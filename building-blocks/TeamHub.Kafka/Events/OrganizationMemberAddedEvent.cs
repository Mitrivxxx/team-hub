namespace TeamHub.Kafka.Events;

public sealed class OrganizationMemberAddedEvent
{
    public const string EventTypeName = "organization.member.added";

    public Guid EventId { get; init; }

    public string EventType { get; init; } = EventTypeName;

    public DateTimeOffset OccurredAt { get; init; }

    public Guid OrganizationId { get; init; }

    public string OrganizationName { get; init; } = string.Empty;

    public Guid UserId { get; init; }

    public Guid AddedByUserId { get; init; }

    public IReadOnlyList<Guid> RoleIds { get; init; } = [];
}
