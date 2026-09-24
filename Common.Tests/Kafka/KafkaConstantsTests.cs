using System.Reflection;
using Common.Constants;
using Xunit;

namespace Common.Tests.Kafka;

public sealed class KafkaConstantsTests
{
    [Fact]
    public void AllTopics_ContainsEveryTopicConstantExactlyOnce()
    {
        var topicConstants = typeof(KafkaConstants)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => Assert.IsType<string>(field.GetRawConstantValue()))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var allTopics = KafkaConstants.AllTopics
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(topicConstants, allTopics);
        Assert.Equal(allTopics.Length, allTopics.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(allTopics, string.IsNullOrWhiteSpace);
    }
}
