using HotChocolate;
using HotChocolate.Authorization;
using team_hub_bff.Graph.Models;
using team_hub_bff.Services;

namespace team_hub_bff.Graph;

[Authorize]
public sealed class Query
{
    /// <summary>List organization members with optional filters.</summary>
    public async Task<IReadOnlyList<OrganizationMemberModel>> OrganizationMembers(
        string organizationId,
        string? roleId,
        string? teamId,
        [Service] IOrganizationMemberClient organizationMemberClient,
        [Service] ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(organizationId, out var parsedOrganizationId))
            throw new GraphQLException("Invalid organizationId.");

        Guid? parsedRoleId = null;
        if (!string.IsNullOrWhiteSpace(roleId))
        {
            if (!Guid.TryParse(roleId, out var roleGuid))
                throw new GraphQLException("Invalid roleId.");
            parsedRoleId = roleGuid;
        }

        Guid? parsedTeamId = null;
        if (!string.IsNullOrWhiteSpace(teamId))
        {
            if (!Guid.TryParse(teamId, out var teamGuid))
                throw new GraphQLException("Invalid teamId.");
            parsedTeamId = teamGuid;
        }

        var actorUserId = currentUserService.GetRequiredUserId();
        return await organizationMemberClient.ListMembersAsync(
            parsedOrganizationId,
            actorUserId,
            parsedRoleId,
            parsedTeamId,
            cancellationToken);
    }
}
