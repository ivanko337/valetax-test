using Commissions.Application.CommissionSchemes;
using Commissions.Controllers.Models;
using Common.Application;
using Microsoft.AspNetCore.Mvc;

namespace Commissions.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CommissionSchemesController(
    ICommandHandler<SwitchCommissionSchemeCommand, CommissionSchemeResult> switchHandler)
    : ControllerBase
{
    [HttpPut("current")]
    [ProducesResponseType<CommissionSchemeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Switch(
        SwitchCommissionSchemeRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(request.SchemaType))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid commission scheme",
                Detail = $"Schema type '{request.SchemaType}' is not supported."
            });
        }

        var scheme = await switchHandler.HandleAsync(
            new SwitchCommissionSchemeCommand(request.SchemaType),
            cancellationToken);

        return Ok(CommissionSchemeResponse.From(scheme));
    }
}
