using GreenDonut;
using Xunit;
using team_hub_bff.Graph.DataLoaders;
using team_hub_bff.Graph.Models;
using team_hub_bff.Services;

namespace team_hub_bff.Tests;

public class UserByIdDataLoaderTests
{
    [Fact]
    public async Task LoadAsync_BatchesDistinctIdsIntoSingleAuthCall()
    {
        var authClient = new FakeAuthUserProfileClient();
        var scheduler = new AutoBatchScheduler();
        var dataLoader = new UserByIdDataLoader(authClient, scheduler, new DataLoaderOptions());

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var task1 = dataLoader.LoadAsync(id1);
        var task2 = dataLoader.LoadAsync(id2);
        await Task.WhenAll(task1, task2);

        Assert.Equal(1, authClient.CallCount);
        Assert.Equal(2, authClient.LastRequestedIds.Count);
        Assert.Equal("Alice", (await task1)?.Name);
        Assert.Equal("Bob", (await task2)?.Name);
    }

    sealed class FakeAuthUserProfileClient : IAuthUserProfileClient
    {
        public int CallCount { get; private set; }
        public IReadOnlyList<Guid> LastRequestedIds { get; private set; } = [];

        public Task<IReadOnlyDictionary<Guid, UserModel>> GetUsersByIdsAsync(
            IReadOnlyList<Guid> userIds,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequestedIds = userIds.ToArray();

            var map = userIds.Select((id, index) => new UserModel
            {
                Id = id,
                Username = $"user{index}",
                Name = index == 0 ? "Alice" : "Bob",
                Surname = "Test"
            }).ToDictionary(u => u.Id);

            return Task.FromResult<IReadOnlyDictionary<Guid, UserModel>>(map);
        }
    }
}
