using team_hub_bff.Graph.DataLoaders;
using team_hub_bff.Graph.Models;

namespace team_hub_bff.Graph;

[ExtendObjectType(typeof(OrganizationMemberModel))]
public sealed class OrganizationMemberExtensions
{
    /// <summary>Resolve user profile from auth via batch DataLoader.</summary>
    public async Task<UserModel?> GetUserAsync(
        [Parent] OrganizationMemberModel member,
        UserByIdDataLoader userByIdDataLoader,
        CancellationToken cancellationToken)
        => await userByIdDataLoader.LoadAsync(member.UserId, cancellationToken);
}
