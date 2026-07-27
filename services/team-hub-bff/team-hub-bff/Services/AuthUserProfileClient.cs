using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using TeamHub.GrpcContracts.Auth.V1;
using team_hub_bff.Configuration;
using team_hub_bff.Graph.Models;

namespace team_hub_bff.Services;

public interface IAuthUserProfileClient
{
    Task<IReadOnlyDictionary<Guid, UserModel>> GetUsersByIdsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default);
}

public sealed class AuthUserProfileClient : IAuthUserProfileClient, IDisposable
{
    readonly GrpcChannel _channel;
    readonly UserProfileService.UserProfileServiceClient _client;

    public AuthUserProfileClient(IOptions<GrpcOptions> options)
    {
        _channel = GrpcChannel.ForAddress(options.Value.Auth);
        _client = new UserProfileService.UserProfileServiceClient(_channel);
    }

    public async Task<IReadOnlyDictionary<Guid, UserModel>> GetUsersByIdsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, UserModel>();

        var request = new GetUsersByIdsRequest();
        request.UserIds.AddRange(userIds.Select(id => id.ToString()));

        var response = await _client.GetUsersByIdsAsync(request, cancellationToken: cancellationToken);

        return response.Users
            .Where(u => Guid.TryParse(u.Id, out _))
            .ToDictionary(
                u => Guid.Parse(u.Id),
                u => new UserModel
                {
                    Id = Guid.Parse(u.Id),
                    Username = u.Username,
                    Name = u.Name,
                    Surname = u.Surname
                });
    }

    public void Dispose() => _channel.Dispose();
}
