namespace TeamHub.Redis;

public readonly record struct RedisDataSegment
{
    public RedisDataSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Segment value cannot be empty.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }
}
