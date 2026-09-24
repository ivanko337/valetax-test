using System.Diagnostics;
using System.Text;
using Common.Kafka;
using Confluent.Kafka;
using Xunit;

namespace Common.Tests.Kafka;

public sealed class KafkaTraceContextTests
{
    private const string TraceParent =
        "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";

    [Fact]
    public void Producer_ContinuesTraceSuppliedByOutbox()
    {
        using var listener = CreateListener();
        IReadOnlyDictionary<string, string> headers =
            new Dictionary<string, string>
            {
                [KafkaTraceContext.TraceParentHeaderName] = TraceParent,
                [KafkaTraceContext.TraceStateHeaderName] = "vendor=value"
            };

        using var activity = KafkaTraceContext.StartProducerActivity(
            "business.created",
            headers);

        Assert.NotNull(activity);
        Assert.Equal("4bf92f3577b34da6a3ce929d0e0e4736", activity.TraceId.ToString());
        Assert.Equal("00f067aa0ba902b7", activity.ParentSpanId.ToString());
        Assert.Equal(ActivityKind.Producer, activity.Kind);

        var outgoingHeaders = KafkaTraceContext.CreateHeaders(activity);
        Assert.Equal(
            activity.Id,
            GetHeader(outgoingHeaders, KafkaTraceContext.TraceParentHeaderName));
        Assert.Equal(
            activity.TraceId.ToString(),
            GetHeader(outgoingHeaders, KafkaTraceContext.TraceIdHeaderName));
    }

    [Fact]
    public void Consumer_ContinuesTraceFromKafkaHeaders()
    {
        using var listener = CreateListener();
        var headers = new Headers
        {
            {
                KafkaTraceContext.TraceParentHeaderName,
                Encoding.UTF8.GetBytes(TraceParent)
            }
        };

        using var activity = KafkaTraceContext.StartConsumerActivity(
            "business.created",
            headers);

        Assert.NotNull(activity);
        Assert.Equal("4bf92f3577b34da6a3ce929d0e0e4736", activity.TraceId.ToString());
        Assert.Equal("00f067aa0ba902b7", activity.ParentSpanId.ToString());
        Assert.Equal(ActivityKind.Consumer, activity.Kind);
    }

    private static ActivityListener CreateListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source =>
                source.Name == KafkaTraceContext.ActivitySourceName,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static string? GetHeader(Headers headers, string name)
    {
        var value = headers.Last(header => header.Key == name).GetValueBytes();
        return value is null ? null : Encoding.UTF8.GetString(value);
    }
}
