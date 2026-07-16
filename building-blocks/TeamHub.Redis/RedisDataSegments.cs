namespace TeamHub.Redis;

public static class RedisDataSegments
{
    public static RedisDataSegment Users { get; } = new("users");
    public static RedisDataSegment AuthLoginAttempts { get; } = new("auth:login-attempts");
    public static RedisDataSegment AuthLoginLockout { get; } = new("auth:login-lockout");

    public static RedisDataSegment Create(string value) => new(value);
}
