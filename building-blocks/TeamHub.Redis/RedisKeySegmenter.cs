namespace TeamHub.Redis;

public sealed class RedisKeySegmenter : IRedisKeySegmenter
{
    private const char Separator = ':';

    public string BuildSegmentedKey(RedisDataSegment segment, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be empty.", nameof(key));
        }

        return $"{segment.Value}{Separator}{key.Trim()}";
    }
}
