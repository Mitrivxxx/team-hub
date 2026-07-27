using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace TeamHub.BlobStorage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTeamHubBlobStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(BlobStorageOptions.SectionName);
        var connectionString = section[nameof(BlobStorageOptions.ConnectionString)]
            ?? configuration.GetConnectionString("blobs");
        if (string.IsNullOrWhiteSpace(connectionString))
            return services;

        services
            .AddOptions<BlobStorageOptions>()
            .Bind(section)
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.ConnectionString))
                    options.ConnectionString = connectionString;
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
            return new BlobServiceClient(options.ConnectionString);
        });

        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        services.AddHostedService<BlobStorageContainerInitializer>();

        return services;
    }
}

sealed class BlobStorageContainerInitializer(BlobServiceClient blobServiceClient, IOptions<BlobStorageOptions> options)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
