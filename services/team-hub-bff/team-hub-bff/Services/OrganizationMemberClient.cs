using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using TeamHub.GrpcContracts.Organization.V1;
using team_hub_bff.Configuration;
using team_hub_bff.Graph.Models;

namespace team_hub_bff.Services;

public interface IOrganizationMemberClient
{
    Task<IReadOnlyList<OrganizationMemberModel>> ListMembersAsync(
        Guid organizationId,
        Guid actorUserId,
        Guid? roleId,
        Guid? teamId,
        CancellationToken cancellationToken = default);

    Task<OrganizationActivityPageModel> ListActivityAsync(
        Guid organizationId,
        Guid actorUserId,
        string? type,
        string? q,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public sealed class OrganizationMemberClient : IOrganizationMemberClient, IDisposable
{
    readonly GrpcChannel _channel;
    readonly OrganizationMemberService.OrganizationMemberServiceClient _client;

    public OrganizationMemberClient(IOptions<GrpcOptions> options)
    {
        _channel = GrpcChannel.ForAddress(options.Value.Organization);
        _client = new OrganizationMemberService.OrganizationMemberServiceClient(_channel);
    }

    public async Task<IReadOnlyList<OrganizationMemberModel>> ListMembersAsync(
        Guid organizationId,
        Guid actorUserId,
        Guid? roleId,
        Guid? teamId,
        CancellationToken cancellationToken = default)
    {
        var request = new ListMembersRequest
        {
            OrganizationId = organizationId.ToString(),
            ActorUserId = actorUserId.ToString()
        };

        if (roleId is not null)
            request.RoleId = roleId.Value.ToString();

        if (teamId is not null)
            request.TeamId = teamId.Value.ToString();

        var response = await _client.ListMembersAsync(request, cancellationToken: cancellationToken);

        return response.Members.Select(m => new OrganizationMemberModel
        {
            UserId = Guid.Parse(m.UserId),
            JoinedAt = DateTimeOffset.Parse(m.JoinedAt),
            TeamIds = m.TeamIds.Select(Guid.Parse).ToArray(),
            Roles = m.Roles.Select(r => new RoleModel
            {
                Id = Guid.Parse(r.Id),
                Name = r.Name,
                Scope = r.Scope,
                IsSystem = r.IsSystem
            }).ToArray()
        }).ToArray();
    }

    public async Task<OrganizationActivityPageModel> ListActivityAsync(
        Guid organizationId,
        Guid actorUserId,
        string? type,
        string? q,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var request = new ListActivityRequest
        {
            OrganizationId = organizationId.ToString(),
            ActorUserId = actorUserId.ToString(),
            Page = page,
            PageSize = pageSize
        };

        if (!string.IsNullOrWhiteSpace(type))
            request.Type = type;
        if (!string.IsNullOrWhiteSpace(q))
            request.Q = q;
        if (from is not null)
            request.From = from.Value.ToString("O");
        if (to is not null)
            request.To = to.Value.ToString("O");

        var response = await _client.ListActivityAsync(request, cancellationToken: cancellationToken);

        return new OrganizationActivityPageModel
        {
            Page = response.Page,
            PageSize = response.PageSize,
            TotalCount = response.TotalCount,
            Items = response.Items.Select(item => new OrganizationActivityModel
            {
                Id = Guid.Parse(item.Id),
                Type = item.Type,
                ActorUserId = item.HasActorUserId ? Guid.Parse(item.ActorUserId) : null,
                TargetUserId = item.HasTargetUserId ? Guid.Parse(item.TargetUserId) : null,
                EntityType = item.HasEntityType ? item.EntityType : null,
                EntityId = item.HasEntityId ? Guid.Parse(item.EntityId) : null,
                Details = item.HasDetails ? item.Details : null,
                OccurredAt = DateTimeOffset.Parse(item.OccurredAt)
            }).ToArray()
        };
    }

    public void Dispose() => _channel.Dispose();
}
