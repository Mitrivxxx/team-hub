using GreenDonut;
using team_hub_bff.Graph.Models;
using team_hub_bff.Services;

namespace team_hub_bff.Graph.DataLoaders;

public sealed class UserByIdDataLoader : BatchDataLoader<Guid, UserModel?>
{
    readonly IAuthUserProfileClient _authClient;

    public UserByIdDataLoader(
        IAuthUserProfileClient authClient,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : base(batchScheduler, options ?? new DataLoaderOptions())
    {
        _authClient = authClient;
    }

    protected override async Task<IReadOnlyDictionary<Guid, UserModel?>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        var users = await _authClient.GetUsersByIdsAsync(keys, cancellationToken);
        var result = new Dictionary<Guid, UserModel?>();

        foreach (var key in keys)
            result[key] = users.TryGetValue(key, out var user) ? user : null;

        return result;
    }
}
