namespace TeamHub.BlobStorage;

public static class BlobStoragePaths
{
    public static string OrganizationAvatar(Guid organizationId, string extension) =>
        $"organizations/{organizationId}/avatar.{extension.TrimStart('.')}";

    public static string TeamAvatar(Guid organizationId, Guid teamId, string extension) =>
        $"organizations/{organizationId}/teams/{teamId}/avatar.{extension.TrimStart('.')}";

    public static bool IsOrganizationAvatarPath(string? blobName) =>
        !string.IsNullOrWhiteSpace(blobName)
        && blobName.StartsWith("organizations/", StringComparison.Ordinal)
        && !blobName.Contains("/teams/", StringComparison.Ordinal);

    public static bool IsTeamAvatarPath(string? blobName) =>
        !string.IsNullOrWhiteSpace(blobName)
        && blobName.Contains("/teams/", StringComparison.Ordinal)
        && blobName.StartsWith("organizations/", StringComparison.Ordinal);
}
