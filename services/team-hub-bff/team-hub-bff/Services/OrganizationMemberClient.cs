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

    public void Dispose() => _channel.Dispose();
}
