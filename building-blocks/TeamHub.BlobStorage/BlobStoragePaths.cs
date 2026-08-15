namespace TeamHub.BlobStorage;

public static class BlobStoragePaths
{
    public static string OrganizationAvatar(Guid organizationId, string extension) =>
        $"organizations/{organizationId}/avatar.{extension.TrimStart('.')}";

    public static string TeamAvatar(Guid organizationId, Guid teamId, string extension) =>
        $"organizations/{organizationId}/teams/{teamId}/avatar.{extension.TrimStart('.')}";

    public static string UserAvatar(Guid userId, string extension) =>
        $"users/{userId}/avatar.{extension.TrimStart('.')}";

    public static string ImportExportSource(Guid organizationId, Guid jobId, string extension) =>
        $"organizations/{organizationId}/import-export/{jobId}/source.{extension.TrimStart('.')}";

    public static string ImportExportResult(Guid organizationId, Guid jobId, string fileBaseName, string extension) =>
        $"organizations/{organizationId}/import-export/{jobId}/{SanitizeFileBaseName(fileBaseName)}.{extension.TrimStart('.')}";

    public static string ImportExportErrors(Guid organizationId, Guid jobId) =>
        $"organizations/{organizationId}/import-export/{jobId}/errors.csv";

    public static string SanitizeFileBaseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "export";

        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Trim().Select(c => invalid.Contains(c) || c is '/' or '\\' ? '-' : c).ToArray();
        var sanitized = new string(chars).Trim(' ', '.', '-');
        return string.IsNullOrWhiteSpace(sanitized) ? "export" : sanitized;
    }
    public static bool IsOrganizationAvatarPath(string? blobName) =>
        !string.IsNullOrWhiteSpace(blobName)
        && blobName.StartsWith("organizations/", StringComparison.Ordinal)
        && !blobName.Contains("/teams/", StringComparison.Ordinal);

    public static bool IsTeamAvatarPath(string? blobName) =>
        !string.IsNullOrWhiteSpace(blobName)
        && blobName.Contains("/teams/", StringComparison.Ordinal)
        && blobName.StartsWith("organizations/", StringComparison.Ordinal);

    public static bool IsUserAvatarPath(string? blobName) =>
        !string.IsNullOrWhiteSpace(blobName)
        && blobName.StartsWith("users/", StringComparison.Ordinal);
}
