namespace TeamHub.BlobStorage;

public interface IBlobStorageService
{
    Task UploadAsync(
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default,
        string? downloadFileName = null);

    Task<Stream> OpenReadAsync(string blobName, CancellationToken cancellationToken = default);

    Task DeleteIfExistsAsync(string blobName, CancellationToken cancellationToken = default);

    Uri? GetReadSasUri(string blobName);
}
