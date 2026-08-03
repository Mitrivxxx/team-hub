using team_hub_bff.Graph.DataLoaders;
using team_hub_bff.Graph.Models;

namespace team_hub_bff.Graph;

[ExtendObjectType(typeof(OrganizationActivityModel))]
public sealed class OrganizationActivityExtensions
{
    /// <summary>Resolve actor profile from auth.</summary>
    public async Task<UserModel?> GetActorAsync(
        [Parent] OrganizationActivityModel activity,
        UserByIdDataLoader userByIdDataLoader,
        CancellationToken cancellationToken)
    {
        if (activity.ActorUserId is not Guid actorUserId)
            return null;

        return await userByIdDataLoader.LoadAsync(actorUserId, cancellationToken);
    }

    /// <summary>Resolve target profile from auth.</summary>
    public async Task<UserModel?> GetTargetAsync(
        [Parent] OrganizationActivityModel activity,
        UserByIdDataLoader userByIdDataLoader,
        CancellationToken cancellationToken)
    {
        if (activity.TargetUserId is not Guid targetUserId)
            return null;

        return await userByIdDataLoader.LoadAsync(targetUserId, cancellationToken);
    }
}
