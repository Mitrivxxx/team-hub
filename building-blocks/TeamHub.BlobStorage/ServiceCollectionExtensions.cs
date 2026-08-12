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
        // Prefer Aspire-injected ConnectionStrings:blobs over local .env BlobStorage:ConnectionString
        // (Azurite host port is dynamic unless AppHost pins it).
        var aspireConnection = configuration.GetConnectionString("blobs");
        var connectionString = FirstNonEmpty(
            aspireConnection,
            section[nameof(BlobStorageOptions.ConnectionString)]);
        if (string.IsNullOrWhiteSpace(connectionString))
            return services;

        var preferAspireConnection = !string.IsNullOrWhiteSpace(aspireConnection);

        services
            .AddOptions<BlobStorageOptions>()
            .Bind(section)
            .PostConfigure(options =>
            {
                options.ConnectionString = connectionString;
                var blobEndpoint = TryGetBlobEndpoint(connectionString);
                if (blobEndpoint is null)
                    return;

                // Keep SAS host/port aligned with the active connection string.
                if (preferAspireConnection || string.IsNullOrWhiteSpace(options.PublicBlobEndpoint))
                    options.PublicBlobEndpoint = blobEndpoint;
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

    static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    /// <summary>Reads BlobEndpoint=... from an Azure Storage connection string (no trailing slash).</summary>
    static string? TryGetBlobEndpoint(string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            const string prefix = "BlobEndpoint=";
            if (!part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var endpoint = part[prefix.Length..].Trim().TrimEnd('/');
            return string.IsNullOrWhiteSpace(endpoint) ? null : endpoint;
        }

        return null;
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
