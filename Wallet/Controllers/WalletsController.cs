using Common.Application;
using Microsoft.AspNetCore.Mvc;
using Wallet.Application.Wallets;
using Wallet.Controllers.Models;

namespace Wallet.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WalletsController(
    IQueryHandler<GetWalletBalanceQuery, WalletBalance?> getBalanceHandler,
    IQueryHandler<GetPayoutHistoryQuery, IReadOnlyList<PayoutHistoryEntry>?> getPayoutHistoryHandler)
    : ControllerBase
{
    [HttpGet("{userExternalId:guid}/balance")]
    [ProducesResponseType<WalletBalanceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance(
        Guid userExternalId,
        CancellationToken cancellationToken)
    {
        var balance = await getBalanceHandler.HandleAsync(
            new GetWalletBalanceQuery(userExternalId),
            cancellationToken);

        return balance is null
            ? WalletNotFound(userExternalId)
            : Ok(WalletBalanceResponse.From(balance));
    }

    [HttpGet("{userExternalId:guid}/payouts")]
    [ProducesResponseType<IReadOnlyList<PayoutResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayoutHistory(
        Guid userExternalId,
        CancellationToken cancellationToken)
    {
        var payouts = await getPayoutHistoryHandler.HandleAsync(
            new GetPayoutHistoryQuery(userExternalId),
            cancellationToken);

        return payouts is null
            ? WalletNotFound(userExternalId)
            : Ok(payouts.Select(PayoutResponse.From).ToArray());
    }

    private NotFoundObjectResult WalletNotFound(Guid userExternalId)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Wallet not found.",
            Detail = $"Wallet for user '{userExternalId}' does not exist."
        };
        problem.Extensions["code"] = "wallet_not_found";

        return NotFound(problem);
    }
}
