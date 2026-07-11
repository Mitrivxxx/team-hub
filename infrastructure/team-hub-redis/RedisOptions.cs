using System.ComponentModel.DataAnnotations;

namespace TeamHub.Redis;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;
}
