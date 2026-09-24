using Commissions.Application.CommissionSchemes;
using Commissions.Controllers;
using Commissions.Controllers.Models;
using Common.Application;
using Common.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Commissions.Tests.Controllers;

public sealed class CommissionSchemesControllerTests
{
    private static readonly DateTimeOffset ChangedAt =
        new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Switch_ReturnsNewCurrentScheme()
    {
        var handler = new SwitchHandler(new CommissionSchemeResult(
            2,
            CommissionSchemaType.Fibonacci,
            ChangedAt));
        var controller = new CommissionSchemesController(handler);

        var response = await controller.Switch(
            new SwitchCommissionSchemeRequest(CommissionSchemaType.Fibonacci),
            CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(response);
        var scheme = Assert.IsType<CommissionSchemeResponse>(result.Value);
        Assert.Equal(2, scheme.Version);
        Assert.Equal(CommissionSchemaType.Fibonacci, scheme.SchemaType);
        Assert.Equal(ChangedAt, scheme.ChangedAt);
        Assert.Equal(CommissionSchemaType.Fibonacci, handler.Command?.SchemaType);
    }

    [Fact]
    public async Task Switch_ReturnsBadRequestForUnsupportedScheme()
    {
        var handler = new SwitchHandler(new CommissionSchemeResult(
            1,
            CommissionSchemaType.Linear,
            ChangedAt));
        var controller = new CommissionSchemesController(handler);

        var response = await controller.Switch(
            new SwitchCommissionSchemeRequest((CommissionSchemaType)99),
            CancellationToken.None);

        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.IsType<ProblemDetails>(result.Value);
        Assert.Null(handler.Command);
    }

    private sealed class SwitchHandler(CommissionSchemeResult result)
        : ICommandHandler<SwitchCommissionSchemeCommand, CommissionSchemeResult>
    {
        public SwitchCommissionSchemeCommand? Command { get; private set; }

        public Task<CommissionSchemeResult> HandleAsync(
            SwitchCommissionSchemeCommand command,
            CancellationToken cancellationToken)
        {
            Command = command;
            return Task.FromResult(result);
        }
    }
}
