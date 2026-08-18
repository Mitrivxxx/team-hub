using System.Diagnostics;

namespace TeamHub.Kafka;

internal static class KafkaActivity
{
    public const string SourceName = "TeamHub.Kafka";

    static readonly ActivitySource Source = new(SourceName);

    public static Activity? StartProduce(string topic)
    {
        var activity = Source.StartActivity($"kafka produce {topic}", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.operation", "publish");
        activity?.SetTag("messaging.destination.name", topic);
        return activity;
    }

    public static Activity? StartConsume(string topic)
    {
        var activity = Source.StartActivity($"kafka consume {topic}", ActivityKind.Consumer);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.operation", "process");
        activity?.SetTag("messaging.destination.name", topic);
        return activity;
    }

    public static void SetError(Activity? activity, Exception exception)
    {
        if (activity is null)
            return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddException(exception);
    }
}
