namespace TeamHub.Redis;

public interface IRedisKeySegmenter
{
    string BuildSegmentedKey(RedisDataSegment segment, string key);
}
