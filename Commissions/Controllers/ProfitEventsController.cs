using Commissions.Application.ProfitEvents;
using Commissions.Controllers.Models;
using Common.Application;
using Microsoft.AspNetCore.Mvc;

namespace Commissions.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProfitEventsController(
    ICommandHandler<ReceiveProfitEventCommand, ReceiveProfitEventResult> receiveHandler,
    IQueryHandler<GetUserProfitEventsQuery, IReadOnlyList<ProfitEventListItem>> getUserEventsHandler,
    IQueryHandler<GetProfitEventDetailsQuery, ProfitEventDetails?> detailsHandler)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ReceiveProfitEventResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Receive(
        ReceiveProfitEventRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ExternalEventId == Guid.Empty
            || request.UserExternalId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid profit event",
                Detail = "externalEventId and userExternalId must be non-empty UUIDs."
            });
        }

        var result = await receiveHandler.HandleAsync(
            new ReceiveProfitEventCommand(
                request.ExternalEventId,
                request.UserExternalId,
                request.ProfitCents,
                request.OccurredAt),
            cancellationToken);

        if (result == ReceiveProfitEventResult.ConflictingDuplicate)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Conflicting profit event",
                Detail = $"External event '{request.ExternalEventId}' was already received with different data."
            });
        }

        return Accepted(new ReceiveProfitEventResponse(
            request.ExternalEventId,
            result == ReceiveProfitEventResult.AlreadyAccepted));
    }

    [HttpGet("{externalEventId:guid}")]
    [ProducesResponseType<ProfitEventDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(
        Guid externalEventId,
        CancellationToken cancellationToken)
    {
        if (externalEventId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid profit event ID.",
                Detail = "externalEventId must be a non-empty UUID."
            });
        }

        var details = await detailsHandler.HandleAsync(
            new GetProfitEventDetailsQuery(externalEventId),
            cancellationToken);

        if (details is null)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Profit event not found.",
                Detail = $"Profit event '{externalEventId}' does not exist."
            };
            problem.Extensions["code"] = "profit_event_not_found";

            return NotFound(problem);
        }

        return Ok(ProfitEventDetailsResponse.From(details));
    }

    [HttpGet("users/{userExternalId:guid}")]
    [ProducesResponseType<IReadOnlyList<ProfitEventResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserProfitEvents(
        Guid userExternalId,
        CancellationToken cancellationToken)
    {
        var events = await getUserEventsHandler.HandleAsync(
            new GetUserProfitEventsQuery(userExternalId),
            cancellationToken);

        return Ok(events.Select(ProfitEventResponse.From).ToArray());
    }
}
