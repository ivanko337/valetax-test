using Common.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wallet.Application.Wallets;
using Wallet.Controllers;
using Wallet.Controllers.Models;
using Xunit;

namespace Wallet.Tests.Controllers;

public sealed class WalletsControllerTests
{
    [Fact]
    public async Task GetBalance_ReturnsWalletBalance()
    {
        var userExternalId = Guid.NewGuid();
        var controller = new WalletsController(
            new BalanceHandler(new WalletBalance(userExternalId, 1_250)),
            new PayoutHistoryHandler([]));

        var response = await controller.GetBalance(
            userExternalId,
            CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(response);
        var balance = Assert.IsType<WalletBalanceResponse>(result.Value);
        Assert.Equal(userExternalId, balance.UserExternalId);
        Assert.Equal(1_250, balance.BalanceCents);
    }

    [Fact]
    public async Task GetPayoutHistory_ReturnsPayouts()
    {
        var userExternalId = Guid.NewGuid();
        var payout = new PayoutHistoryEntry(
            Guid.NewGuid(),
            800,
            DateTimeOffset.UtcNow);
        var controller = new WalletsController(
            new BalanceHandler(new WalletBalance(userExternalId, 800)),
            new PayoutHistoryHandler([payout]));

        var response = await controller.GetPayoutHistory(
            userExternalId,
            CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(response);
        var payouts = Assert.IsType<PayoutResponse[]>(result.Value);
        var actualPayout = Assert.Single(payouts);
        Assert.Equal(payout.CommissionId, actualPayout.CommissionId);
        Assert.Equal(payout.AmountCents, actualPayout.AmountCents);
        Assert.Equal(payout.PaidAt, actualPayout.PaidAt);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WalletQuery_ReturnsNotFoundForMissingWallet(bool getBalance)
    {
        var userExternalId = Guid.NewGuid();
        var controller = new WalletsController(
            new BalanceHandler(null),
            new PayoutHistoryHandler(null));

        var response = getBalance
            ? await controller.GetBalance(userExternalId, CancellationToken.None)
            : await controller.GetPayoutHistory(userExternalId, CancellationToken.None);

        var result = Assert.IsType<NotFoundObjectResult>(response);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal("wallet_not_found", problem.Extensions["code"]);
    }

    private sealed class BalanceHandler(WalletBalance? result)
        : IQueryHandler<GetWalletBalanceQuery, WalletBalance?>
    {
        public Task<WalletBalance?> HandleAsync(
            GetWalletBalanceQuery query,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }
    }

    private sealed class PayoutHistoryHandler(IReadOnlyList<PayoutHistoryEntry>? result)
        : IQueryHandler<GetPayoutHistoryQuery, IReadOnlyList<PayoutHistoryEntry>?>
    {
        public Task<IReadOnlyList<PayoutHistoryEntry>?> HandleAsync(
            GetPayoutHistoryQuery query,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }
    }
}
