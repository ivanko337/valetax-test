using Commissions.Application.CommissionSchemes;
using Commissions.Domain.CommissionSchemes;
using Common.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Commissions.Tests.Application.CommissionSchemes;

public sealed class SwitchCommissionSchemeHandlerTests
{
    private static readonly DateTimeOffset ChangedAt =
        new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_AppendsNewSchemeVersion()
    {
        var repository = new StubRepository();
        var handler = new SwitchCommissionSchemeHandler(
            repository,
            new FixedTimeProvider(ChangedAt),
            NullLogger<SwitchCommissionSchemeHandler>.Instance);

        var result = await handler.HandleAsync(
            new SwitchCommissionSchemeCommand(CommissionSchemaType.Fibonacci),
            CancellationToken.None);

        Assert.NotNull(repository.Added);
        Assert.Equal(CommissionSchemaType.Fibonacci, repository.Added.SchemaType);
        Assert.Equal(ChangedAt, repository.Added.ChangedAt);
        Assert.Equal(CommissionSchemaType.Fibonacci, result.SchemaType);
        Assert.Equal(ChangedAt, result.ChangedAt);
    }

    [Fact]
    public async Task HandleAsync_RejectsUnsupportedScheme()
    {
        var handler = new SwitchCommissionSchemeHandler(
            new StubRepository(),
            new FixedTimeProvider(ChangedAt),
            NullLogger<SwitchCommissionSchemeHandler>.Instance);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => handler.HandleAsync(
            new SwitchCommissionSchemeCommand((CommissionSchemaType)99),
            CancellationToken.None));
    }

    private sealed class StubRepository : ICommissionSchemeRepository
    {
        public CommissionScheme? Added { get; private set; }

        public Task AddAsync(
            CommissionScheme commissionScheme,
            CancellationToken cancellationToken)
        {
            Added = commissionScheme;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
