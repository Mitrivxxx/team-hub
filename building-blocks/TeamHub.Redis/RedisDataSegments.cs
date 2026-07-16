namespace TeamHub.Redis;

public static class RedisDataSegments
{
    public static RedisDataSegment Users { get; } = new("users");

    public static RedisDataSegment Create(string value) => new(value);
}
