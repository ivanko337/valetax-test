using Common.Persistence;
using Xunit;

namespace Common.Tests.Persistence;

public sealed class EnumExtensionsTests
{
    [Fact]
    public void BuildDescription_ReturnsStableDescriptionWithLfLineEndings()
    {
        const string expected =
            "Possible values of enum type: TestState\n" +
            "----------\n" +
            "0 -- Pending\n" +
            "1 -- Completed\n" +
            "2 -- Failed\n";

        var result = EnumExtensions.BuildDescription<TestState>();

        Assert.Equal(expected, result);
    }

    private enum TestState : byte
    {
        Pending = 0,
        Completed = 1,
        Failed = 2
    }
}
