using System.Diagnostics;
using System.Text;
using Confluent.Kafka;

namespace Common.Kafka;

public static class KafkaTraceContext
{
    public const string ActivitySourceName = "Valetax.Messaging.Kafka";
    public const string TraceIdHeaderName = "trace-id";
    public const string TraceParentHeaderName = "traceparent";
    public const string TraceStateHeaderName = "tracestate";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    internal static Activity StartProducerActivity(
        string topic,
        IReadOnlyDictionary<string, string>? headers)
    {
        var traceParent = GetHeaderValue(headers, TraceParentHeaderName);
        var traceState = GetHeaderValue(headers, TraceStateHeaderName);
        var parentContext = ParseParentContext(traceParent, traceState);
        var activity = parentContext is { } parent
            ? ActivitySource.StartActivity(
                $"{topic} publish",
                ActivityKind.Producer,
                parent)
            : ActivitySource.StartActivity(
                $"{topic} publish",
                ActivityKind.Producer);

        activity ??= StartFallbackActivity(
            $"{topic} publish",
            parentContext is null ? null : traceParent,
            traceState);

        activity.SetTag("messaging.system", "kafka");
        activity.SetTag("messaging.destination.name", topic);
        activity.SetTag("messaging.operation.type", "publish");

        return activity;
    }

    internal static Activity StartConsumerActivity(string topic, Headers? headers)
    {
        var traceParent = GetHeaderValue(headers, TraceParentHeaderName);
        var traceState = GetHeaderValue(headers, TraceStateHeaderName);
        traceParent ??= CreateTraceParent(
            GetHeaderValue(headers, TraceIdHeaderName));
        var parentContext = ParseParentContext(traceParent, traceState);
        var activity = parentContext is { } parent
            ? ActivitySource.StartActivity(
                $"{topic} process",
                ActivityKind.Consumer,
                parent)
            : ActivitySource.StartActivity(
                $"{topic} process",
                ActivityKind.Consumer);

        activity ??= StartFallbackActivity(
            $"{topic} process",
            parentContext is null ? null : traceParent,
            traceState);

        activity.SetTag("messaging.system", "kafka");
        activity.SetTag("messaging.destination.name", topic);
        activity.SetTag("messaging.operation.type", "process");

        return activity;
    }

    internal static Headers CreateHeaders(Activity activity)
    {
        var headers = new Headers();
        headers.Add(TraceIdHeaderName, Encoding.UTF8.GetBytes(activity.TraceId.ToString()));

        if (activity.Id is not null)
        {
            headers.Add(TraceParentHeaderName, Encoding.UTF8.GetBytes(activity.Id));
        }

        if (activity.TraceStateString is not null)
        {
            headers.Add(
                TraceStateHeaderName,
                Encoding.UTF8.GetBytes(activity.TraceStateString));
        }

        return headers;
    }

    private static string? GetHeaderValue(Headers? headers, string name)
    {
        var value = headers?
            .LastOrDefault(header => string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
            ?.GetValueBytes();

        return value is null ? null : Encoding.UTF8.GetString(value);
    }

    private static string? GetHeaderValue(
        IReadOnlyDictionary<string, string>? headers,
        string name)
    {
        return headers?
            .FirstOrDefault(pair => string.Equals(
                pair.Key,
                name,
                StringComparison.OrdinalIgnoreCase))
            .Value;
    }

    private static ActivityContext? ParseParentContext(
        string? traceParent,
        string? traceState)
    {
        return ActivityContext.TryParse(
            traceParent,
            traceState,
            isRemote: true,
            out var context)
                ? context
                : null;
    }

    private static Activity StartFallbackActivity(
        string name,
        string? traceParent,
        string? traceState)
    {
        var activity = new Activity(name).SetIdFormat(ActivityIdFormat.W3C);

        if (traceParent is not null)
        {
            activity.SetParentId(traceParent);
        }

        activity.TraceStateString = traceState;
        return activity.Start();
    }

    private static string? CreateTraceParent(string? traceId)
    {
        if (traceId is null || traceId.Length != 32)
        {
            return null;
        }

        try
        {
            var parsedTraceId = ActivityTraceId.CreateFromString(traceId.AsSpan());

            if (parsedTraceId == default)
            {
                return null;
            }

            return $"00-{parsedTraceId}-{ActivitySpanId.CreateRandom()}-01";
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
