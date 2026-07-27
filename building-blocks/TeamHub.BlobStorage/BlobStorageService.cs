using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace TeamHub.BlobStorage;

public sealed class BlobStorageService : IBlobStorageService
{
    readonly BlobContainerClient _containerClient;
    readonly BlobStorageOptions _options;

    public BlobStorageService(BlobServiceClient blobServiceClient, IOptions<BlobStorageOptions> options)
    {
        _options = options.Value;
        _containerClient = blobServiceClient.GetBlobContainerClient(_options.ContainerName);
    }

    public async Task UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            },
            cancellationToken);
    }

    public async Task DeleteIfExistsAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public Uri? GetReadSasUri(string blobName)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        if (!blobClient.CanGenerateSasUri)
            return null;

        var expiresOn = DateTimeOffset.UtcNow.AddMinutes(_options.SasExpiryMinutes);
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = expiresOn
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);
        if (string.IsNullOrWhiteSpace(_options.PublicBlobEndpoint))
            return sasUri;

        var publicUri = new Uri(_options.PublicBlobEndpoint.TrimEnd('/') + "/");
        var builder = new UriBuilder(sasUri)
        {
            Scheme = publicUri.Scheme,
            Host = publicUri.Host,
            Port = publicUri.Port
        };

        return builder.Uri;
    }
}
