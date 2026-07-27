using System.ComponentModel.DataAnnotations;

namespace TeamHub.BlobStorage;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string ContainerName { get; set; } = "avatars";

    [Required]
    public string PublicBlobEndpoint { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int SasExpiryMinutes { get; set; } = 60;
}
